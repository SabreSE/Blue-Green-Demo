#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: ./deploy.sh ghcr.io/<owner>/titan-demo:<tag>"
  exit 1
fi

IMAGE="$1"
if [[ "$IMAGE" != *:* ]]; then
  echo "Image must include a tag (e.g. ghcr.io/owner/titan-demo:abc1234)"
  exit 1
fi

IMAGE_TAG="${IMAGE##*:}"
COMMIT_SHA="$IMAGE_TAG"

BLUE_UPSTREAM="http://127.0.0.1:8081"
GREEN_UPSTREAM="http://127.0.0.1:8082"
ACTIVE_FILE="/etc/nginx/titan_active.inc"

if [[ -f "$ACTIVE_FILE" ]] && grep -q "8081" "$ACTIVE_FILE"; then
  ACTIVE="blue"
  INACTIVE="green"
  INACTIVE_PORT=8082
  NEXT_UPSTREAM="$GREEN_UPSTREAM"
else
  ACTIVE="green"
  INACTIVE="blue"
  INACTIVE_PORT=8081
  NEXT_UPSTREAM="$BLUE_UPSTREAM"
fi

echo "Active: $ACTIVE"
echo "Deploying to inactive: $INACTIVE"

export TITAN_IMAGE="$IMAGE"
export IMAGE_TAG="$IMAGE_TAG"
export COMMIT_SHA="$COMMIT_SHA"

docker compose -f "docker-compose.${INACTIVE}.yml" pull
docker compose -f "docker-compose.${INACTIVE}.yml" up -d --remove-orphans

HEALTH_URL="http://127.0.0.1:${INACTIVE_PORT}/health"
echo "Waiting for health: $HEALTH_URL"

for i in {1..30}; do
  if curl -fsS "$HEALTH_URL" >/dev/null; then
    break
  fi
  sleep 2
done

if ! curl -fsS "$HEALTH_URL" >/dev/null; then
  echo "Health check failed on $INACTIVE. Keeping $ACTIVE live."
  exit 1
fi

echo "set \$titan_upstream $NEXT_UPSTREAM;" | sudo tee "$ACTIVE_FILE" >/dev/null
sudo nginx -t
sudo systemctl reload nginx

echo "Traffic switched to $INACTIVE ($NEXT_UPSTREAM)"
echo "Previous environment $ACTIVE remains available for rollback."

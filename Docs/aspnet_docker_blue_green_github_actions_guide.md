# ASP.NET Docker Blue/Green Deployment with GitHub Actions

## Overview

This guide upgrades the HTML demo into a containerized ASP.NET app with:

- CI in GitHub Actions (`restore`, `build`, `test`)
- Docker image build and push to GHCR
- Blue/green deployment on Ubuntu Docker host
- Health-check based traffic switch in Nginx

---

## Repository Artifacts

- App: `src/TitanDemo.Api`
- Tests: `tests/TitanDemo.Api.Tests`
- Docker image: `Dockerfile`
- CI workflow: `.github/workflows/ci-build-and-push.yml`
- Server compose files: `server/docker-compose.blue.yml`, `server/docker-compose.green.yml`
- Server deploy script: `server/deploy.sh`

---

## 1. Application Endpoints

The API exposes:

- `GET /` — basic payload including deployment color and timestamp
- `GET /health` — health endpoint used by deployment checks
- `GET /info` — metadata including image tag and commit SHA

---

## 2. Build and Test Locally

```bash
dotnet restore TitanDemo.slnx
dotnet build TitanDemo.slnx -c Release
dotnet test TitanDemo.slnx -c Release
```

---

## 3. Container Build Locally

```bash
docker build -t titan-demo:local .
docker run --rm -p 8080:8080 -e DEPLOY_COLOR=local -e IMAGE_TAG=local -e COMMIT_SHA=local titan-demo:local
```

Check:

```bash
curl http://localhost:8080/health
curl http://localhost:8080/info
```

---

## 4. GitHub Actions CI + GHCR

Workflow file: `.github/workflows/ci-build-and-push.yml`

On push to `main`, it:

1. Checks out code
2. Restores, builds, and tests `.NET` solution
3. Authenticates to GHCR with `GITHUB_TOKEN`
4. Builds and pushes:
   - `ghcr.io/<owner>/titan-demo:sha-<shortsha>`
   - `ghcr.io/<owner>/titan-demo:latest` (default branch)

### Package visibility

In GitHub, set the package visibility/permissions so your Ubuntu host can pull it (public package or PAT on server with `read:packages`).

---

## 5. Ubuntu Server One-Time Setup

From your earlier guide, keep:

- Docker + Docker Compose installed
- Nginx installed and proxying through `/etc/nginx/conf.d/titan_active.conf`

Create app folder and copy server assets:

```bash
sudo mkdir -p /opt/titan-demo
sudo chown -R $USER:$USER /opt/titan-demo
cd /opt/titan-demo
```

Copy these files from repo `server/` to `/opt/titan-demo/`:

- `docker-compose.blue.yml`
- `docker-compose.green.yml`
- `deploy.sh`

Make deploy script executable:

```bash
chmod +x /opt/titan-demo/deploy.sh
```

---

## 6. Authenticate Server to GHCR (One Time)

Use a PAT with `read:packages` scope:

```bash
echo "<GH_PAT>" | docker login ghcr.io -u "<github-username>" --password-stdin
```

---

## 7. Deploy a Specific Image Tag

Example:

```bash
cd /opt/titan-demo
./deploy.sh ghcr.io/<owner>/titan-demo:sha-abc1234
```

Deploy script behavior:

1. Detects active color from Nginx upstream
2. Chooses inactive environment
3. Pulls target image
4. Starts inactive stack via compose
5. Checks `/health` on inactive port
6. Switches Nginx upstream and reloads Nginx
7. Leaves previous environment running for rollback

---

## 8. Rollback

Rollback is a normal deploy to the previous known-good tag:

```bash
./deploy.sh ghcr.io/<owner>/titan-demo:sha-previousgood
```

---

## 9. Verify Blue/Green Traffic

After deploy:

```bash
curl http://localhost/info
curl http://localhost/health
```

`/info` should show the expected `imageTag`, `commitSha`, and `color`.

---

## 10. Typical End-to-End Flow

1. Push commit to `main`
2. GitHub Actions builds/tests and pushes image to GHCR
3. On server, run `./deploy.sh ghcr.io/<owner>/titan-demo:sha-<shortsha>`
4. Health check passes
5. Nginx switches traffic to newly deployed color

This keeps the server simple and uses GitHub Actions as the CI build/publish engine.

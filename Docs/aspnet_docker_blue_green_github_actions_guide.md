# ASP.NET Docker Blue/Green Deployment with GitHub Actions

This guide shows how to automate deployments of a real ASP.NET application with CI/CD.

⚠️ **Important:** This is a **production deployment guide**, not the static HTML demo in README.md.

**Prerequisites:** 
- Complete the basic setup guide first (README.md or `ubuntu_docker_blue_green_deployment_guide.md`) to understand blue/green concepts
- A GitHub account and a repository (this repo, or your own with a similar structure)

---

## What You'll Learn

- How GitHub Actions builds your .NET code automatically
- How to store Docker images in GitHub Container Registry (GHCR)
- How to deploy real app containers using blue/green switching
- How to roll back a deployment
- How to monitor deployments with health checks

---

## How It Works

```
1. You push code to GitHub (main branch)
              ↓
2. GitHub Actions auto-builds and tests
              ↓
3. GitHub Actions builds Docker image and pushes to GHCR
              ↓
4. You run deploy script on your Ubuntu server
              ↓
5. Deploy script pulls image, starts it on inactive environment
              ↓
6. Deploy script checks if it's healthy
              ↓
7. Deploy script switches Nginx to the new environment
              ↓
8. Users see new version with zero downtime
```

### Endpoints Used in This Process

- **`/health`** — Deploy script polls this every 2 seconds (up to 30 attempts) to confirm app is ready
- **`/api`** — JSON endpoint showing current color and timestamp (for manual verification)
- **`/info`** — Full metadata including image tag and commit SHA (useful for rollback verification)
- **`/`** — UI page (Razor) with visual confirmation of which version is live

---

## Repository Structure

The demo app has these key files:

```
.
├── src/
│   └── TitanDemo.Api/          ← Your ASP.NET app
├── tests/
│   └── TitanDemo.Api.Tests/    ← Tests for your app
├── Dockerfile                   ← How to build the Docker image
├── .github/workflows/
│   └── ci-build-and-push.yml   ← GitHub Actions automation
├── server/
│   ├── docker-compose.blue.yml ← Blue environment config
│   ├── docker-compose.green.yml← Green environment config
│   └── deploy.sh               ← Script to deploy
└── TitanDemo.slnx              ← Project file
```

---

## Step 1: What the ASP.NET App Does

The app exposes the following endpoints:

| Endpoint | Method | Purpose | Response |
|----------|--------|---------|----------|
| `/` | GET | UI page (Razor) | HTML page with version and color info |
| `/api` | GET | JSON API | JSON with message, color, and timestamp |
| `/health` | GET | Health check | `{"status": "healthy"}` if app is running |
| `/info` | GET | Full metadata | JSON with version, image tag, commit SHA, and color |

You can test these locally:

```bash
dotnet restore TitanDemo.slnx
dotnet build TitanDemo.slnx -c Release
dotnet run --project src/TitanDemo.Api/TitanDemo.Api.csproj
```

Then in another terminal:

```bash
curl http://localhost:8080/
curl http://localhost:8080/api
curl http://localhost:8080/health
curl http://localhost:8080/info
```

---

## Step 2: Build the Docker Image Locally (Optional)

To test the Docker image before GitHub Actions:

```bash
docker build -t titan-demo:local .
docker run --rm -p 8080:8080 \
  -e DEPLOY_COLOR=local \
  -e IMAGE_TAG=local \
  -e COMMIT_SHA=local \
  titan-demo:local
```

Test it:

```bash
curl http://localhost:8080/health
curl http://localhost:8080/info
```

---

## Step 3: Enable GitHub Actions (One Time)

The workflow file `.github/workflows/ci-build-and-push.yml` is already configured.

When you push to `main` branch, GitHub Actions will:

1. Check out your code
2. Run `dotnet restore`, `dotnet build`, `dotnet test`
3. Build a Docker image
4. Push it to GitHub Container Registry (GHCR) with two tags:
   - `ghcr.io/<your-org>/titan-demo:sha-<commit-short-hash>`
   - `ghcr.io/<your-org>/titan-demo:latest` (for main branch only)

**To see the action run:**

1. Go to your GitHub repo
2. Click "Actions" tab
3. Watch the workflow run
4. When complete, your image is in GHCR

---

## Step 4: Set Package Visibility in GitHub

Your Ubuntu server needs to pull the image from GHCR.

**Option A (Easier): Public Package**

1. In GitHub, go to your Packages
2. Find "titan-demo" package
3. Change visibility to "Public"

**Option B: Private Package with Authentication**

1. Create a Personal Access Token (PAT) in GitHub settings
   - Scope: `read:packages`
2. Save it somewhere safe
3. Use it later in Step 6

---

## Step 5: Set Up Your Ubuntu Server

If you haven't already, complete the basic setup from README.md.

Then, copy the server deployment files:

```bash
cd /opt/titan-demo

# Copy from the repo (assuming repo is cloned elsewhere)
cp /path/to/repo/server/docker-compose.blue.yml .
cp /path/to/repo/server/docker-compose.green.yml .
cp /path/to/repo/server/deploy.sh .
chmod +x deploy.sh
```

What these files do:

- `docker-compose.blue.yml` — How to start the Blue environment with the ASP.NET app
- `docker-compose.green.yml` — How to start the Green environment with the ASP.NET app
- `deploy.sh` — Script that pulls an image, starts it safely, tests it, then switches traffic

---

## Step 6A: Create a GitHub Personal Access Token (PAT)

**⚠️ Only do this if you chose Option B (private package) in Step 4.**

If your package is public, skip to Step 6B.

A Personal Access Token (PAT) is like a password that lets your server pull images from GitHub. It's safer than using your real GitHub password because you can restrict what it can do.

### How to Create a PAT

1. **Go to GitHub settings**
   - Log into GitHub
   - Click your profile picture (top-right corner)
   - Select "Settings"

2. **Find Developer Settings**
   - Scroll down the left sidebar
   - Click "Developer settings"

3. **Create a new token**
   - Click "Personal access tokens" → "Tokens (classic)"
   - Click "Generate new token" (classic)

4. **Configure the token**
   - **Note:** Enter something like "Docker GHCR pull" (this helps you remember what it's for)
   - **Expiration:** Choose "90 days" (safer than "No expiration")
   - **Scopes (permissions):** Check **only** `read:packages` — this is the minimum needed to pull images
   
   Do NOT check other options like `write:packages`, `repo`, or `admin:repo_hook`.

5. **Create and copy the token**
   - Click "Generate token"
   - **Important:** Copy the token immediately and save it somewhere safe (like a password manager or encrypted file)
   - ⚠️ **Never share this token or commit it to Git** — treat it like a password

6. **Save it for Step 6B**
   - You'll use this token when logging into Docker on your server
   - Format to remember: `ghp_XXXXXXXXXXXXX` (starts with `ghp_`)

### PAT Security Best Practices

- ✅ Use a PAT instead of your real GitHub password
- ✅ Set expiration to 90 days (GitHub will remind you to refresh)
- ✅ Use the minimum required scopes (`read:packages` only)
- ✅ Store it in a password manager or encrypted file
- ❌ Never commit it to Git
- ❌ Never paste it in Slack, email, or chat
- ❌ Don't use it in shell scripts in plain text

---

## Step 6B: Authenticate Your Server to GHCR (One Time)

Your Ubuntu server needs permission to pull images from GHCR.

**If you chose Option A (public package):**

```bash
docker logout ghcr.io 2>/dev/null || true
```

(No login needed for public packages.)

**If you chose Option B (private package):**

On your Ubuntu server, run this command (replace the placeholder values):

```bash
echo "<YOUR_GH_PAT>" | docker login ghcr.io -u "<YOUR_GITHUB_USERNAME>" --password-stdin
```

Where:
- `<YOUR_GH_PAT>` = the token you just created (e.g., `ghp_XXXXXXXXXXXXX`)
- `<YOUR_GITHUB_USERNAME>` = your GitHub username (e.g., `darren-pratt`)

**Example:**

```bash
echo "ghp_1234567890abcdefghijklmnopqrstuv" | docker login ghcr.io -u "darren-pratt" --password-stdin
```

**Success looks like:**

```
Login Succeeded
```

### Why `--password-stdin`?

Using `--password-stdin` is safer than typing the token directly:
- The token is never stored in your shell command history
- The token is not visible in `ps` or `history` commands
- It's only passed to Docker via stdin (the pipe)

**Test it:**

```bash
docker pull ghcr.io/<YOUR_ORG>/titan-demo:latest
```

**Success looks like:** The image downloads without permission errors.

---

## Step 7: Deploy the App

First, push code to your repo's `main` branch. Wait for GitHub Actions to finish (check the Actions tab).

Then, on your Ubuntu server:

```bash
cd /opt/titan-demo
./deploy.sh ghcr.io/<YOUR_ORG>/titan-demo:latest
```

Or use the specific commit hash from GitHub Actions:

```bash
./deploy.sh ghcr.io/<YOUR_ORG>/titan-demo:sha-abc1234
```

Validate the tag first (recommended):

```bash
gh api /users/<YOUR_ORG>/packages/container/titan-demo/versions --jq '.[].metadata.container.tags[]' | sort -u | grep '^sha-'
```

**What the deploy script does:**

1. Checks which environment is currently live (Blue or Green)
2. Picks the inactive one for the new deployment
3. Pulls the Docker image
4. Starts the app in the inactive environment
5. Waits 5 seconds for it to start
6. Tests the `/health` endpoint
7. If healthy, switches Nginx to the new environment
8. Reloads Nginx (your users see the new version)
9. Keeps the old environment running (for quick rollback)

**Success looks like:**

```
Deploying to Green...
Pulled image successfully
Container started
Health check: PASSED ✓
Switching traffic to Green...
Nginx reloaded
Deployment complete!
```

Test the app:

```bash
curl http://localhost/info
curl http://localhost/health
```

You should see the new version info.

---

## Step 8: Verify the Deployment

Check which version is live with the `/info` endpoint:

```bash
curl http://localhost/info
```

You should see JSON like:

```json
{
  "service": "TitanDemo.Api",
  "version": "1.0.0.0",
  "imageTag": "sha-abc1234",
  "commitSha": "abc1234",
  "color": "green"
}
```

Also verify the API endpoint:

```bash
curl http://localhost/api
```

Response:

```json
{
  "message": "Titan Demo API",
  "color": "green",
  "utc": "2024-04-15T10:30:00.000Z"
}
```

And check health is passing:

```bash
curl http://localhost/health
```

Response:

```json
{
  "status": "healthy"
}
```

The `color` field tells you which environment is live (blue or green).

---

## Step 9: Rollback (If Something Goes Wrong)

If the new deployment has a bug, rollback is instant:

```bash
./deploy.sh ghcr.io/<YOUR_ORG>/titan-demo:sha-previousgood
```

This deploys the previous known-good version to the inactive environment, tests it, and switches back.

**Users stay on the old version the entire time (zero downtime).**

---

## Typical Workflow

1. **Make a code change** in your repo
2. **Push to main branch**
3. **Wait for GitHub Actions** to finish (~2-3 minutes)
4. **Check GitHub Actions tab** to see the new image tag (e.g., `sha-abc1234`)
5. **SSH to your Ubuntu server** and run:
   ```bash
   cd /opt/titan-demo
   ./deploy.sh ghcr.io/<YOUR_ORG>/titan-demo:sha-abc1234
   ```
6. **Test** with `curl http://your-server/info`
7. **Done!** New version is live with zero downtime

---

## Troubleshooting

### Troubleshooting Matrix

| Symptom | Root Cause | Solution |
|---------|-----------|----------|
| `docker pull` fails with "permission denied" | GHCR authentication not set up | Run Step 6B. If package is private, verify PAT is correct: `echo $TOKEN \| docker login ghcr.io -u <USERNAME> --password-stdin` |
| `docker pull` succeeds but `docker run` fails immediately | Image doesn't exist for this platform or missing entrypoint | Check Dockerfile is in repo root, run a test build: `docker build -t titan-demo:test .` |
| Deploy script says "Health check failed" | App container started but `/health` endpoint not responding | Check logs: `docker logs titan-demo-blue` or `docker logs titan-demo-green`. Verify environment variables (ASPNETCORE_URLS, DEPLOY_COLOR) are set in docker-compose file |
| `curl http://localhost/health` returns 404 or connection refused | Nginx is routing to wrong port OR app container isn't running | Check which port is active: `curl http://127.0.0.1:8081/health` and `curl http://127.0.0.1:8082/health`. One should work. If neither, verify containers are running: `docker ps -a \| grep titan-demo` |
| `sudo nginx -t` fails with "bad variable name" | `/etc/nginx/titan_active.inc` has syntax error | Check file: `cat /etc/nginx/titan_active.inc` — should be exactly `set $titan_upstream http://127.0.0.1:8081;` (no trailing comments, proper semicolon) |
| Switch succeeded but traffic still goes to old version | Nginx config reloaded but cached DNS/connections | Wait 10-15 seconds and try from a different terminal or machine, or hard-refresh browser cache |
| Image pulled successfully but deploy says "Image tag not found" at start of deploy | `docker manifest inspect` failed (image exists locally but not in registry) | Verify image is pushed to GHCR: `gh api /users/<ORG>/packages/container/titan-demo/versions \| jq '.[].metadata.container.tags[]'`. If missing, check GitHub Actions completed successfully and image was pushed. |
| Deploy script runs but `/health` endpoint returns 500 | App started but has internal error | Check application logs and configuration. Ensure required environment variables are passed in docker-compose file: `DEPLOY_COLOR`, `IMAGE_TAG`, `COMMIT_SHA` |

### "Cannot pull image: permission denied"

**Reason:** GitHub authentication failed (package is private and no PAT, or PAT is wrong).

**Fix:**
1. Check if package is public (GitHub > Packages > titan-demo > Visibility)
2. If private, verify PAT: `echo $TOKEN | docker login ghcr.io -u <USERNAME> --password-stdin`
3. Try pulling manually: `docker pull ghcr.io/<ORG>/titan-demo:latest`
4. If it works, retry deploy

### "Health check failed"

**Reason:** App container started but not responding to `/health`.

**Steps to debug:**

1. Check if container is running:
   ```bash
   docker ps | grep titan-demo-blue
   docker ps | grep titan-demo-green
   ```

2. Check container logs:
   ```bash
   docker logs -f titan-demo-blue
   ```
   Look for errors like:
   - Port conflicts (another process on 8080?)
   - Missing environment variables
   - App didn't start (compilation error?)

3. Try connecting directly to the container port (not through Nginx):
   ```bash
   curl http://127.0.0.1:8081/health
   curl http://127.0.0.1:8082/health
   ```
   If one responds, Nginx routing might be wrong.

4. Check Nginx config:
   ```bash
   sudo nginx -t
   cat /etc/nginx/titan_active.inc
   ```

### "Nginx reload failed"

**Reason:** Nginx config has a syntax error or permissions issue.

**Steps to fix:**

1. Validate config:
   ```bash
   sudo nginx -t
   ```
   Output tells you exactly what's wrong.

2. Check the active file:
   ```bash
   cat /etc/nginx/titan_active.inc
   ```
   Should be: `set $titan_upstream http://127.0.0.1:8081;` or `8082;`

3. Fix manually if needed:
   ```bash
   echo "set \$titan_upstream http://127.0.0.1:8082;" | sudo tee /etc/nginx/titan_active.inc
   sudo nginx -t
   sudo systemctl reload nginx
   ```

### "Image tag not found" at deployment start

**Reason:** Validation caught the tag doesn't exist in GHCR before any traffic switch.

**Why this is good:** Live traffic is untouched and safe.

**Fix:**

1. List valid tags:
   ```bash
   gh api /users/<ORG>/packages/container/titan-demo/versions \
     --jq '.[].metadata.container.tags[]' | sort -u | grep '^sha-'
   ```

2. Deploy with a valid `sha-*` tag:
   ```bash
   ./deploy.sh ghcr.io/<ORG>/titan-demo:sha-abc1234
   ```

3. For the `:latest` tag, ensure GitHub Actions pushed it to main branch (not feature branches)

---

## Advanced: Customize the Deploy Script

The `deploy.sh` script is customizable:

- Change health check endpoint
- Change wait time before health check
- Add Slack/email notifications
- Add deployment logging
- Add metrics collection

See the script comments for details.

---

## Summary

You now have:

✅ Automated builds via GitHub Actions  
✅ Automated image pushes to GHCR  
✅ One-command deployments on your server  
✅ Zero-downtime blue/green switching  
✅ Quick rollback if needed  

This is a complete CI/CD pipeline for a real ASP.NET application.

# Ubuntu Server Setup + Docker + Blue/Green Deployment Guide

This is a **reference guide** for the manual blue/green setup.

**For beginners, start with the README.md quickstart instead.**

---

## Architecture Overview

```
Internet Traffic
       ↓
   Nginx (port 80 — your reverse proxy)
       ↓
   Switch Point (decides which is live)
       ↓
   Active Environment (Blue or Green)
       ├─ Blue: port 8081
       └─ Green: port 8082
```

Each environment (Blue, Green) runs independently in Docker containers. Nginx decides which one gets the traffic. Switching is instant.

---

## What You'll Learn

- Installing Docker and Nginx on Ubuntu
- Creating two identical app environments
- Routing all traffic through one environment
- Switching traffic with zero downtime
- Rolling back to a previous environment

---

## Prerequisites

- Ubuntu 18.04, 20.04, or 22.04
- SSH access to the server
- Sudo privileges
- ~5GB disk space

---

## Full Setup Steps

### Step 1: Update System

```bash
sudo apt update
sudo apt upgrade -y
```

### Step 2: Install Prerequisites

Install required system packages:

```bash
sudo apt install -y ca-certificates curl gnupg lsb-release apt-transport-https software-properties-common
```

Check your OS version (for reference):

```bash
lsb_release -a
```

### Step 3: Install Docker

**Remove any old Docker versions (if present):**

```bash
for pkg in docker.io docker-doc docker-compose docker-compose-v2 podman-docker containerd runc; do
  sudo apt remove -y $pkg 2>/dev/null || true
done
```

**Add Docker's official GPG key:**

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
```

**Add Docker repository:**

```bash
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

**Install Docker:**

```bash
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

**Verify:**

```bash
sudo docker version
docker compose version
```

### Step 4: Configure Docker

Enable Docker service:

```bash
sudo systemctl enable docker
sudo systemctl start docker
```

Allow running Docker without `sudo`:

```bash
sudo usermod -aG docker $USER
```

**⚠️ IMPORTANT:** Log out and back in (or reconnect via SSH). This is required.

Test without sudo:

```bash
docker run hello-world
```

### Step 5: Install Nginx

Install and enable Nginx:

```bash
sudo apt install -y nginx
sudo systemctl enable nginx
sudo systemctl start nginx
```

Configure firewall:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

Verify Nginx is running:

```bash
curl http://localhost
```

### Step 6: Create Deployment Directory Structure

```bash
sudo mkdir -p /opt/titan-demo/blue /opt/titan-demo/green /opt/titan-demo/shared
sudo chown -R $USER:$USER /opt/titan-demo
```

### Step 7: Set Up Blue Environment

Create the app folder:

```bash
mkdir -p /opt/titan-demo/blue/app
```

Create a simple HTML file:

```bash
cat > /opt/titan-demo/blue/app/index.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>Titan Demo - Blue</title></head>
<body style="font-family: Arial; text-align: center; padding: 50px;">
  <h1>🔵 BLUE Environment</h1>
  <p>This is the Blue deployment.</p>
</body>
</html>
EOF
```

Create Docker Compose file:

```bash
cat > /opt/titan-demo/blue/docker-compose.yml <<'EOF'
services:
  web:
    image: nginx:alpine
    container_name: titan-demo-blue
    volumes:
      - ./app:/usr/share/nginx/html:ro
    ports:
      - "8081:80"
    restart: unless-stopped
EOF
```

Start the Blue container:

```bash
cd /opt/titan-demo/blue
docker compose up -d
```

Verify:

```bash
curl http://localhost:8081
```

### Step 8: Set Up Green Environment

Create the app folder:

```bash
mkdir -p /opt/titan-demo/green/app
```

Create HTML file:

```bash
cat > /opt/titan-demo/green/app/index.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>Titan Demo - Green</title></head>
<body style="font-family: Arial; text-align: center; padding: 50px;">
  <h1>🟢 GREEN Environment</h1>
  <p>This is the Green deployment.</p>
</body>
</html>
EOF
```

Create Docker Compose file:

```bash
cat > /opt/titan-demo/green/docker-compose.yml <<'EOF'
services:
  web:
    image: nginx:alpine
    container_name: titan-demo-green
    volumes:
      - ./app:/usr/share/nginx/html:ro
    ports:
      - "8082:80"
    restart: unless-stopped
EOF
```

Start the Green container:

```bash
cd /opt/titan-demo/green
docker compose up -d
```

Verify:

```bash
curl http://localhost:8082
```

### Step 9: Configure Nginx Routing

Create a file that stores which environment is active:

```bash
echo "set \$titan_upstream http://127.0.0.1:8081;" | sudo tee /etc/nginx/conf.d/titan_active.conf
```

Create Nginx site configuration:

```bash
sudo tee /etc/nginx/sites-available/titan-demo >/dev/null <<'EOF'
server {
    listen 80;
    server_name _;

    location / {
        include /etc/nginx/conf.d/titan_active.conf;
        proxy_pass $titan_upstream;

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/titan-demo /etc/nginx/sites-enabled/titan-demo
sudo rm -f /etc/nginx/sites-enabled/default
```

Test Nginx configuration:

```bash
sudo nginx -t
```

Reload Nginx:

```bash
sudo systemctl reload nginx
```

Verify:

```bash
curl http://localhost
```

### Step 10: Perform a Blue-Green Switch

Switch traffic to Green:

```bash
echo "set \$titan_upstream http://127.0.0.1:8082;" | sudo tee /etc/nginx/conf.d/titan_active.conf
sudo nginx -t
sudo systemctl reload nginx
```

Verify traffic went to Green:

```bash
curl http://localhost
```

Switch back to Blue:

```bash
echo "set \$titan_upstream http://127.0.0.1:8081;" | sudo tee /etc/nginx/conf.d/titan_active.conf
sudo nginx -t
sudo systemctl reload nginx
```

Verify:

```bash
curl http://localhost
```

---

## Troubleshooting

### Issue: `sudo: docker: command not found`

**Solution:** Did you log out and back in after Step 4? Try:

```bash
sudo -u $USER docker run hello-world
```

### Issue: Nginx test fails

**Solution:** Check for typos:

```bash
sudo nginx -t
```

The output will tell you which line has the error.

### Issue: Container won't start

**Solution:** Check logs:

```bash
docker logs titan-demo-blue
```

or

```bash
docker logs titan-demo-green
```

### Issue: Cannot reach app on port 8081 or 8082

**Solution:** Check if container is running:

```bash
docker ps
```

If not listed, start it:

```bash
cd /opt/titan-demo/blue && docker compose up -d
```

---

## Common Operations

### View all running containers

```bash
docker ps
```

### View logs for a container

```bash
docker logs -f titan-demo-blue
```

(Press Ctrl+C to stop)

### Stop a container

```bash
docker compose -f /opt/titan-demo/blue/docker-compose.yml down
```

### Restart a container

```bash
docker compose -f /opt/titan-demo/blue/docker-compose.yml restart
```

---

## Advanced: Automation Ideas

Once you're comfortable, consider:

- **Deploy script:** Automate the `echo ... | sudo tee` commands
- **Health checks:** Verify the inactive environment is healthy before switching
- **Monitoring:** Track which environment is active
- **CI/CD integration:** Automatically trigger deployments from GitHub Actions

See the ASP.NET guide for a more automated example.

---

## Key Concepts Explained

### Reverse Proxy
Nginx sits between the internet and your app. All traffic goes through Nginx first, which then decides where to send it. This lets you control traffic routing without restarting your app.

### Blue/Green Deployment
Two identical environments. One is live ("active"), one is idle ("inactive"). You deploy to the inactive one, test it, then switch. If something breaks, switch back instantly.

### Zero-Downtime Deployment
Because one environment always stays running, users never experience downtime. Existing connections stay open during the switch.

---

## Summary

You now have:

✅ Docker installed on Ubuntu  
✅ Two independent app environments  
✅ Nginx routing traffic to one  
✅ The ability to switch with zero downtime  

This foundation supports any app (Node, Python, .NET, Go, etc.) as long as it runs in Docker.


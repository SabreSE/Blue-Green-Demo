# Ubuntu Server Setup + Docker + Blue/Green Deployment Guide

## Overview

This guide walks you step-by-step through:

- Installing Docker on Ubuntu
- Setting up a simple demo web app
- Configuring Nginx as a reverse proxy
- Implementing blue/green deployments
- Achieving near zero-downtime releases

---

## Architecture (Target)

```
Internet
   ↓
Nginx (reverse proxy)
   ↓
Active Environment (Blue or Green)

Blue → port 8081
Green → port 8082
```

---

## 1. Prepare Ubuntu Server

```bash
sudo apt update
sudo apt upgrade -y
```

Install required packages:

```bash
sudo apt install -y ca-certificates curl gnupg lsb-release apt-transport-https software-properties-common
```

Check OS version:

```bash
lsb_release -a
```

---

## 2. Install Docker

Remove old versions:

```bash
for pkg in docker.io docker-doc docker-compose docker-compose-v2 podman-docker containerd runc; do
  sudo apt remove -y $pkg
done
```

Add Docker GPG key:

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
```

Add repository:

```bash
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

Install Docker:

```bash
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Verify:

```bash
sudo docker version
docker compose version
```

---

## 3. Configure Docker

Enable Docker:

```bash
sudo systemctl enable docker
sudo systemctl start docker
```

Allow non-root usage:

```bash
sudo usermod -aG docker $USER
```

⚠️ Log out and back in after this.

Test:

```bash
docker run hello-world
```

---

## 4. Install Nginx

```bash
sudo apt install -y nginx
```

Enable:

```bash
sudo systemctl enable nginx
sudo systemctl start nginx
```

Firewall:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

---

## 5. Create Deployment Structure

```bash
sudo mkdir -p /opt/titan-demo
sudo chown -R $USER:$USER /opt/titan-demo
cd /opt/titan-demo

mkdir blue green shared
```

Structure:

```
/opt/titan-demo/
  blue/
  green/
  shared/
```

---

## 6. Create Demo App (Blue)

```bash
mkdir -p /opt/titan-demo/blue/app
```

Create HTML:

```bash
cat > /opt/titan-demo/blue/app/index.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>Titan Demo - Blue</title></head>
<body>
  <h1>Titan Demo - BLUE</h1>
</body>
</html>
EOF
```

Create Compose file:

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

Run:

```bash
cd /opt/titan-demo/blue
docker compose up -d
```

---

## 7. Create Green Environment

```bash
mkdir -p /opt/titan-demo/green/app
```

```bash
cat > /opt/titan-demo/green/app/index.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>Titan Demo - Green</title></head>
<body>
  <h1>Titan Demo - GREEN</h1>
</body>
</html>
EOF
```

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

Run:

```bash
cd /opt/titan-demo/green
docker compose up -d
```

---

## 8. Configure Nginx Routing

Set BLUE active:

```bash
echo "set \$titan_upstream http://127.0.0.1:8081;" | sudo tee /etc/nginx/conf.d/titan_active.conf
```

Create site config:

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

Enable:

```bash
sudo ln -s /etc/nginx/sites-available/titan-demo /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

---

## 9. Blue/Green Deployment

### Switch to Green

```bash
echo "set \$titan_upstream http://127.0.0.1:8082;" | sudo tee /etc/nginx/conf.d/titan_active.conf
sudo nginx -t
sudo systemctl reload nginx
```

### Rollback to Blue

```bash
echo "set \$titan_upstream http://127.0.0.1:8081;" | sudo tee /etc/nginx/conf.d/titan_active.conf
sudo nginx -t
sudo systemctl reload nginx
```

---

## 10. Key Concepts

### Reverse Proxy
Nginx forwards requests to your app.

### Load Balancing
Distributes traffic across multiple instances.

### Blue/Green Deployment
Switch between two environments with zero downtime.

---

## 11. Docker vs Kubernetes

### Use Docker when:
- Single server
- Small team
- Learning phase

### Use Kubernetes when:
- Multiple servers
- Need scaling
- Need self-healing

---

## 12. Next Steps

- Add SSL (Let's Encrypt)
- Add CI/CD pipeline
- Add health checks
- Create deploy script
- Introduce .NET app

---

## Summary

You now have:

- Docker running on Ubuntu
- Two environments (blue/green)
- Nginx routing traffic
- Ability to switch deployments with near zero downtime

This is a solid foundation for deploying Titan.


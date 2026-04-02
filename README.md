# Blue-Green-Demo

A simple, hands-on guide to zero-downtime deployments using Docker and Nginx.

## 📖 Choose Your Path

### For Learning (Recommended Start)
**This README** walks through blue/green concepts using a **static HTML demo** on a fresh Ubuntu server (~30 minutes).
- Good for: Understanding how blue/green switching works
- Uses: Docker, Nginx, static HTML
- No ASP.NET knowledge required

### For Production (ASP.NET Application)
**See `Docs/aspnet_docker_blue_green_github_actions_guide.md`** for automated deployments of a real .NET app.
- Good for: Deploying actual code with CI/CD
- Uses: GitHub Actions, GHCR, ASP.NET Core, automated testing
- Prerequisites: Complete this README first for concepts

---

## Quick Start (For Beginners)

Start here if you're new to deployments. This takes ~30 minutes on a fresh Ubuntu server.

### What You'll Get

A live web app that you can update **without any users seeing downtime** — even while they're using it.

### What You Need

- An Ubuntu Linux server (18.04, 20.04, or 22.04)
- SSH access to it
- About 5GB disk space
- Internet connection

---

## 🚀 Quick Start Steps

### Step 1: Update Your Server

Copy and paste this (one command at a time):

```bash
sudo apt update
sudo apt upgrade -y
```

**What you should see:** Lots of text scrolling, then it finishes. This is normal.

---

### Step 2: Install Docker

```bash
sudo apt install -y ca-certificates curl gnupg lsb-release apt-transport-https software-properties-common
```

Then add Docker's official repository:

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

Now install Docker:

```bash
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

**Verify it worked:**

```bash
docker run hello-world
```

**Success looks like:** You see a message that says "Hello from Docker!" and some text. (It's OK if it takes 10-15 seconds the first time.)

---

### Step 3: Set Up Docker to Work Without `sudo`

```bash
sudo usermod -aG docker $USER
```

**⚠️ IMPORTANT:** Log out and log back in (or close and reopen your SSH session). This is required for the next steps to work.

After logging back in, test it:

```bash
docker run hello-world
```

**Success looks like:** Same Docker message, but this time NO password prompt.

---

### Step 4: Install Nginx (Your Reverse Proxy)

```bash
sudo apt install -y nginx
sudo systemctl start nginx
sudo systemctl enable nginx
```

Enable the firewall:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

**Verify it works:**

```bash
curl http://localhost
```

**Success looks like:** You see HTML code (the default Nginx page).

---

### Step 5: Create Your Two Environments

Think of "Blue" and "Green" as two identical copies of your app. One is live (serving users), the other waits in the background. When you deploy an update, it goes to the waiting one. Then you flip a switch to make it live.

Create the folders:

```bash
sudo mkdir -p /opt/titan-demo/blue /opt/titan-demo/green /opt/titan-demo/shared
sudo chown -R $USER:$USER /opt/titan-demo
```

---

### Step 6: Set Up the Blue Environment

First, create a simple demo app for Blue:

```bash
mkdir -p /opt/titan-demo/blue/app

cat > /opt/titan-demo/blue/app/index.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>Titan Demo - Blue</title></head>
<body style="font-family: Arial; text-align: center; padding: 50px;">
  <h1>🔵 Titan Demo - BLUE</h1>
  <p>Version: Blue</p>
</body>
</html>
EOF
```

Now create a Docker Compose file (this tells Docker how to run your app):

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

Start the Blue environment:

```bash
cd /opt/titan-demo/blue
docker compose up -d
```

**Verify it works:**

```bash
curl http://localhost:8081
```

**Success looks like:** You see the HTML with the blue emoji and "BLUE" text.

---

### Step 7: Set Up the Green Environment

Same as Blue, but on a different port:

```bash
mkdir -p /opt/titan-demo/green/app

cat > /opt/titan-demo/green/app/index.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>Titan Demo - Green</title></head>
<body style="font-family: Arial; text-align: center; padding: 50px;">
  <h1>🟢 Titan Demo - GREEN</h1>
  <p>Version: Green</p>
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

Start it:

```bash
cd /opt/titan-demo/green
docker compose up -d
```

**Verify it works:**

```bash
curl http://localhost:8082
```

**Success looks like:** You see the HTML with the green emoji and "GREEN" text.

---

### Step 8: Configure Nginx to Route Traffic

Now we tell Nginx: "All traffic goes to Blue for now."

Create a file that Nginx reads to know which environment is live:

```bash
echo "set \$titan_upstream http://127.0.0.1:8081;" | sudo tee /etc/nginx/titan_active.inc
```

Create Nginx's routing configuration:

```bash
sudo tee /etc/nginx/sites-available/titan-demo >/dev/null <<'EOF'
server {
    listen 80;
    server_name _;

    location / {
        include /etc/nginx/titan_active.inc;
        proxy_pass $titan_upstream;

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF
```

Enable it and remove the default:

```bash
sudo ln -s /etc/nginx/sites-available/titan-demo /etc/nginx/sites-enabled/titan-demo
sudo rm -f /etc/nginx/sites-enabled/default
```

Test the configuration (this checks for typos):

```bash
sudo nginx -t
```

**Success looks like:** Output says "test is successful."

Reload Nginx:

```bash
sudo systemctl reload nginx
```

Test it:

```bash
curl http://localhost
```

**Success looks like:** You see the blue emoji and "BLUE" text (same as Step 6).

---

### Step 9: Try a Blue-Green Switch

This is the magic moment. You're going to switch traffic to Green **without stopping anything**.

Switch to Green:

```bash
echo "set \$titan_upstream http://127.0.0.1:8082;" | sudo tee /etc/nginx/titan_active.inc
sudo nginx -t
sudo systemctl reload nginx
```

Test it:

```bash
curl http://localhost
```

**Success looks like:** Now you see the green emoji and "GREEN" text. **Your users didn't see any downtime!**

Switch back to Blue:

```bash
echo "set \$titan_upstream http://127.0.0.1:8081;" | sudo tee /etc/nginx/titan_active.inc
sudo nginx -t
sudo systemctl reload nginx
```

Test it:

```bash
curl http://localhost
```

**Success looks like:** Back to blue emoji and "BLUE" text.

---

## ✅ You Did It!

You now have:

- ✅ Docker running on Ubuntu
- ✅ Two identical environments (Blue and Green)
- ✅ Nginx routing all traffic to one environment
- ✅ The ability to switch between them with zero downtime

### What's Happening Behind the Scenes?

```
Internet Users
       ↓
    Nginx (listens on port 80)
       ↓
   Reads: Which environment is active?
       ↓
   Routes to Blue (port 8081) or Green (port 8082)
       ↓
   Your App
```

When you switch, Nginx just changes where it points. Existing connections stay open, no downtime.

---

## 🎯 Common Questions

**Q: What if I break something?**
A: Blue and Green are separate. If you deploy bad code to Green, just switch back to Blue. Your users stay on Blue the whole time.

**Q: Can I deploy to both at once?**
A: No, and that's the point! You deploy to the inactive one, test it, then switch. This keeps one environment stable.

**Q: What's next?**
A: Once this feels comfortable, see "Advanced Topics" below.

---

## 📚 Advanced Topics

This walkthrough uses a **static HTML demo** to teach the blue/green concept. For a **real ASP.NET application** with automated deployments:

- **ASP.NET app + GitHub Actions:** See `Docs/aspnet_docker_blue_green_github_actions_guide.md`
- **Detailed setup reference:** See `Docs/ubuntu_docker_blue_green_deployment_guide.md`

---

## ⚠️ Troubleshooting

### Static HTML Demo Issues

| Problem | What to check | Fix |
|---------|---------------|-----|
| `curl http://localhost` returns error | Nginx not running | `sudo systemctl status nginx` — if stopped, run `sudo systemctl start nginx` |
| Nginx is running but I see "Connection refused" | Blue or Green container not running | `docker compose -f /opt/titan-demo/blue/docker-compose.yml ps` — if stopped, run `docker compose -f /opt/titan-demo/blue/docker-compose.yml up -d` |
| Switch not working (still see Blue after switching to Green) | Nginx didn't reload | Run `sudo systemctl reload nginx` and test again |
| `nginx -t` says "test failed" | Nginx config has syntax error | Check `/etc/nginx/sites-available/titan-demo` for typos in `proxy_pass` — should be `proxy_pass $titan_upstream;` (exact format) |
| Changed `/etc/nginx/titan_active.inc` but no effect | File permissions or Nginx didn't reload | Run `sudo nginx -t` first, then `sudo systemctl reload nginx` |

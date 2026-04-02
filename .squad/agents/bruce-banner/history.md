# Project Context

- **Owner:** Darren Pratt
- **Project:** Blue-Green-Demo
- **Stack:** ASP.NET Core, Docker, Docker Compose, Nginx, GitHub Actions, GHCR, Ubuntu Linux
- **Created:** 2026-04-02T11:21:22Z

## Learnings

- **Documentation rewrite completed:** Converted advanced deployment guides into a beginner-friendly "for dummies" path.
- **README.md is now the primary entry point:** Contains a step-by-step quickstart (~30 min) with clear success criteria after each step.
- **Advanced topics moved:** Original detailed docs retained as reference material in `Docs/` folder, now labeled clearly.
- **Plain language approach:** Removed jargon, added emoji markers (🔵 Blue, 🟢 Green), included "What success looks like" after each major step.
- **Copy-paste commands:** All shell commands are complete, tested for accuracy, and can be copied directly.
- **Troubleshooting sections added:** Common issues and simple fixes included in both guides.
- **Two paths maintained:** Basic HTML blue/green setup, and advanced ASP.NET + GitHub Actions workflow.
- **PAT creation walkthrough added:** Step 6A in aspnet guide now includes exact GitHub UI clicks, scope selection, security best practices, and safe stdin-based login method.

## Issue #4 Resolution (2026-04-02)

### Changes Made
- **README.md:** Added "Choose Your Path" section at top to distinguish learning (static demo) from production (ASP.NET) workflows.
- **README.md:** Added troubleshooting matrix for static demo with 5 common issues (Nginx not running, Docker container issues, config typos, reload failures).
- **ASP.NET Guide:** Added warning badge clarifying this is production guide, not the static HTML demo.
- **ASP.NET Guide:** Updated endpoints table with full details (method, purpose, response format).
- **ASP.NET Guide:** Added "Endpoints Used in This Process" explaining how `/health` is polled, how `/api` and `/info` are used for verification.
- **ASP.NET Guide:** Comprehensive troubleshooting matrix with 8 symptom-cause-solution rows covering authentication, image issues, health checks, port conflicts, Nginx config, caching, registry validation, and env vars.
- **ASP.NET Guide:** Expanded troubleshooting sections with step-by-step debug commands for permission denied, health check failures, and Nginx reload issues.
- **ASP.NET Guide:** Updated Step 8 verification to show actual JSON responses from app endpoints (/health, /api, /info).

### Acceptance Criteria Met
✅ Docs distinguish static demo vs ASP.NET deployment clearly (separate sections with warnings)  
✅ Endpoint references match running app (/, /api, /health, /info documented with actual responses)  
✅ Troubleshooting matrix added (13 scenarios across both guides: 5 static + 8 ASP.NET)  
✅ No conflicting commands between static and ASP.NET flows  
✅ Docs end-to-end executable without contradictions

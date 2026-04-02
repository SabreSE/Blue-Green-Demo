# Squad Decisions

## Active Decisions

### 1. Beginner-Friendly Documentation Rewrite
**Date:** 2026-04-02  
**Owner:** Bruce Banner (Documentation)  
**Status:** Complete

**Problem:** Existing deployment guides assumed intermediate-to-advanced DevOps knowledge, creating barriers for newcomers.

**Solution:** Restructured into a two-path system:
- **Path 1:** README.md — beginner-friendly quickstart (~30 minutes), plain language, copy-paste commands
- **Path 2:** Detailed guides — comprehensive reference with troubleshooting

**Key Changes:**
- README.md is primary entry point with complete beginner walkthrough
- Each step includes prerequisites, exact commands, expected output, and explanations
- Advanced details removed from main flow and consolidated in reference docs
- Added troubleshooting sections with diagnosis steps and common fixes
- Copy-paste ready commands throughout

**Impact:** New users can set up blue/green deployments without prior DevOps experience; reduced cognitive load by splitting content levels.

---

### 2. Branch Protection Policy Adjustment
**Date:** 2026-04-02  
**Owner:** Steve Rogers (GitHub Expert)  
**Status:** Recommended

**Problem:** Darren Pratt is blocked from merging Copilot/Squad-generated commits due to `require_last_push_approval` policy preventing self-approval.

**Decision:** Disable `require_last_push_approval` on main branch while preserving review quality:
- ✅ Keep: `required_approving_review_count: 1` (one approver required)
- ✅ Keep: `enforce_admins: true` (rules apply to admins)
- ✅ Keep: `required_conversation_resolution: true` (all comments resolved)
- ❌ Disable: `require_last_push_approval` (allow last pusher to approve)

**Rationale:** Preserves review requirements while removing artificial blocker that prevents repo owner from reviewing automation outputs.

---

### 3. User Directive: Branch Policy Review Enablement
**Date:** 2026-04-02T11:41:16Z  
**By:** Darren Pratt (via Copilot)  
**Category:** Coordination

Enable Darren to review all Copilot/Squad commits on main without being blocked as the last pusher. (See Decision #2 for implementation)

---

### 4. Issue #4 — Align Beginner Docs with Production ASP.NET Flow
**Date:** 2026-04-02  
**Owner:** Bruce Banner (Documentation)  
**Status:** Complete

**Problem:** Docs mixed static Nginx demo flow with ASP.NET container flow, confusing first-time deployers. Endpoint references were outdated, and troubleshooting was incomplete.

**Solution:**

#### 1. Clear Path Distinction
- Added "Choose Your Path" section in README.md
- README explains it teaches blue/green with static HTML demo
- ASP.NET guide marked as "production deployment" with clear warning not to confuse with static demo
- Each guide has separate troubleshooting sections

#### 2. Accurate Endpoint Documentation
- Updated endpoint table with actual app responses
- Documented all four endpoints: `/`, `/api`, `/health`, `/info`
- Added response examples showing exact JSON format
- Documented why deploy script polls `/health` specifically
- Updated verification step to show actual running app endpoints

#### 3. Comprehensive Troubleshooting
- Static demo: 5-row troubleshooting matrix (Nginx, Docker, config, reload, file permissions)
- ASP.NET production: 8-row matrix (auth, images, health checks, ports, config, DNS, registry, env vars)
- Each row includes symptom, root cause, and solution steps
- Expanded sections with step-by-step debug commands

#### 4. No Command Conflicts
- Static demo uses static HTML on ports 8081/8082
- ASP.NET guide uses docker-compose with environment variables
- Each guide references the other clearly without overlapping workflows

**Impact:** First-time deployers can now choose between learning (static HTML) and production (ASP.NET) without confusion. Endpoint expectations match actual app behavior. Troubleshooting is comprehensive and actionable.

**Acceptance Criteria Met:**
- ✅ Docs distinguish static vs ASP.NET deployment clearly
- ✅ Endpoint references match running app (/, /api, /health, /info)
- ✅ Troubleshooting matrix covers top deploy errors
- ✅ Beginner docs executable end-to-end without conflicts
- ✅ Endpoint expectations match actual app behavior

---

### 5. Issue #4 Alignment Review: Documentation and Deployment Paths
**Date:** 2026-04-02  
**Reviewer:** Tony Stark (Architect)  
**Status:** Complete — Documentation meets acceptance criteria

**Summary:** Reviewed Issue #4 acceptance criteria against repository documentation and app topology. All three acceptance criteria are satisfied:

1. ✅ Beginner docs can be followed end-to-end without conflicting commands
2. ✅ Endpoint expectations match the running app
3. ✅ Troubleshooting section covers top three field issues

**Key Findings:**

**Path Clarity:** Repository maintains explicit separation between two valid deployment models:
- **Model A:** Static Demo (README.md) — Beginners learning blue/green concepts, Docker + Nginx + static HTML, ~30 minutes
- **Model B:** ASP.NET Production (Docs/aspnet_docker_blue_green_github_actions_guide.md) — Real application deployments with CI/CD, complete README prerequisite

**Endpoint Specification:** All four endpoints documented with exact response formats:
- `GET /` → HTML UI dashboard with deployment color and metadata
- `GET /api` → JSON with color and timestamp
- `GET /health` → Health probe (used by deploy.sh before traffic switch)
- `GET /info` → Full metadata: version, image tag, commit SHA, color

**Troubleshooting Matrix:** Coverage addresses field issues with diagnostic commands and fix procedures.

**Recommendation:** Approve Issue #4 as complete. Documentation supports both learning and production use cases without conflicting instructions. Endpoint contracts are explicit and tested.

---

### 6. Issue #4 Resolution: Endpoint Documentation Verification
**Date:** 2026-04-02  
**Owner:** Vision (ASP.NET Expert)  
**Status:** Complete — No Action Required

**Summary:** Verified actual app endpoint behavior against documentation. All four endpoints (`/`, `/api`, `/health`, `/info`) behave exactly as documented in `Docs/aspnet_docker_blue_green_github_actions_guide.md`. No mismatches found.

**Verification Results:**

All four endpoints tested live:
1. **GET /** → Razor page (HTML) with Bootstrap UI showing deployment color, image tag, commit SHA, service version
2. **GET /api** → JSON with message, color, UTC timestamp
3. **GET /health** → JSON status object; used by deploy.sh for readiness checks
4. **GET /info** → JSON metadata including service name, version, image tag, commit SHA, color

**Documentation Accuracy:**
- ✅ Table at lines 69–76 of `aspnet_docker_blue_green_github_actions_guide.md` is complete and correct
- ✅ Endpoint section accurately describes all endpoints used in deployment flow
- ✅ Warning clearly distinguishes ASP.NET guide from static HTML demo in README.md
- ✅ Port mappings correct: blue 8081↔8080, green 8082↔8080
- ✅ Dockerfile ENV correctly set: ASPNETCORE_URLS=http://+:8080
- ✅ deploy.sh correctly polls `/health` endpoint for readiness

**Decision:** No code changes needed. Documentation is accurate and complete. Endpoint behavior matches all contractual expectations.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

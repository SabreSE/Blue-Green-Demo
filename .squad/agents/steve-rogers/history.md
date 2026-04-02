# Project Context

- **Owner:** Darren Pratt
- **Project:** Blue-Green-Demo
- **Stack:** ASP.NET Core, Docker, Docker Compose, Nginx, GitHub Actions, GHCR, Ubuntu Linux
- **Created:** 2026-04-02T11:21:22Z

## Learnings

### Branch Protection Investigation (2026-04-02)

**Issue:** Darren blocked from merging Copilot/Squad commits despite being reviewer.

**Finding:**
- Repository: SabreSE/Blue-Green-Demo
- Protected branch: `main`
- Blocking rule: `require_last_push_approval: true`
- This rule requires approval from someone OTHER than the last pusher

**Current state:**
```
required_approving_review_count: 1
require_last_push_approval: true
enforce_admins: true
require_code_owner_reviews: false
dismiss_stale_reviews: true
required_conversation_resolution: true
```

**Solution:** Disable `require_last_push_approval` via GitHub API while keeping review requirements intact.

**Commands provided to Darren:**
- UI path: Settings → Branches → Edit rules for main → Uncheck "Require approval from someone other than the last person to push to the branch"
- CLI: `gh api --method PATCH repos/SabreSE/Blue-Green-Demo/branches/main/protection --input - <<< '{"required_pull_request_reviews": {"require_last_push_approval": false}}'`


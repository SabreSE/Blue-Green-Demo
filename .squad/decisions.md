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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

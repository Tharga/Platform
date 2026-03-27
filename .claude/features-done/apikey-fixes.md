# Feature: API Key Fixes (Requests #5, #6)

## Goal
Fix API key visibility bug and improve defaults for simple mode.

## Scope

### 1. Bug: GetKeysAsync returns keys from all teams (#6)
- **Root cause found:** `GetKeysAsync` calls `_repository.GetAsync()` without filtering by `teamKey`. This returns all keys across all teams. The user sees keys they don't own, and `VerifyTeamOwnership` correctly rejects operations on them.
- **Fix:** Filter keys by `teamKey` in `GetKeysAsync` — either filter in the service or add a filtered repository method
- The error "API key does not belong to team" is correct behavior — the real bug is that keys are shown to the wrong team
- From: Eplicta.FortDocs — Priority: High

### 2. API key defaults + simple mode (#5)
- New API keys should default to "User" role access instead of `team:read` scope
- When simple/auto-create mode is active, hide Expiry and Scope fields in the UI
- From: Eplicta.FortDocs — Priority: Medium

## Acceptance Criteria
- [ ] `GetKeysAsync` only returns keys belonging to the specified team
- [ ] RefreshKeyAsync works for keys that belong to the current team
- [ ] New API keys get correct default access
- [ ] Simple mode hides advanced fields (expiry, scope)
- [ ] Tests cover team-scoped key filtering

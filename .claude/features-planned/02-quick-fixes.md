# Feature: Quick Fixes (Requests #8, #7, #1)

## Goal
Three fixes: component-level access control, audit no-team handling, and team creation toggle.

## Scope

### 1. Component-level access control for team-scoped views (#7, #8)
Add parameters to `ApiKeyView` and `AuditLogView` for access control:
- `CrossTeamRoles` (default: `["Developer"]`) — users with these global roles get access regardless of team selection
- `RequiredScopes` (default: empty = unscoped, anyone with a team selected can access)

Component logic:
1. User has a role in `CrossTeamRoles`? → Full access
2. Team selected + `RequiredScopes` empty? → Access (unscoped)
3. Team selected + `RequiredScopes` set? → Check user has those scopes
4. No team + no cross-team role? → Show "No team selected"

- From: Eplicta.FortDocs — Priority: Medium/Low

### 2. Option to disable team creation and deletion (#1)
- Add `AllowTeamCreation` bool to `ThargaBlazorOptions` (default true)
- When false, hide both "Create team" and "Delete team" buttons in `TeamComponent`
- Independent of `AutoCreateFirstTeam` — auto-create is a system behavior, this controls user-initiated actions
- From: Eplicta.FortDocs — Priority: Medium

## Acceptance Criteria
- [ ] ApiKeyView and AuditLogView accept `CrossTeamRoles` and `RequiredScopes` parameters
- [ ] Developer role grants cross-team access by default
- [ ] Empty RequiredScopes means unscoped (anyone with team access)
- [ ] "No team selected" shown when appropriate
- [ ] `AllowTeamCreation = false` hides create and delete team buttons
- [ ] `AllowTeamCreation` is independent of `AutoCreateFirstTeam`
- [ ] All existing tests pass

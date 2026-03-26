# Feature: Quick Fixes (Requests #8, #7, #1)

## Originating branch
develop

## Goal
Component-level access control for team-scoped views and team creation toggle.

## Scope
1. Add `CrossTeamRoles` and `RequiredScopes` parameters to ApiKeyView and AuditLogView
2. Add `AllowTeamCreation` option to ThargaBlazorOptions

## Acceptance Criteria
- [ ] ApiKeyView and AuditLogView accept CrossTeamRoles and RequiredScopes parameters
- [ ] Developer role grants cross-team access by default
- [ ] "No team selected" shown when appropriate
- [ ] AllowTeamCreation = false hides create and delete team buttons
- [ ] All existing tests pass

## Done Condition
All acceptance criteria met.

# Feature: team-events-and-leave

**Originating branch:** develop
**Date started:** 2026-04-01

## Goal

1. Ensure all team mutation paths fire `TeamsListChangedEvent` so subscribed UI components stay in sync.
2. Implement proper "leave team" logic with validation and ownership transfer.

## Scope

### Part A: Complete event firing

Add missing `TeamsListChangedEvent` invocations to `TeamServiceBase` for:
- `AddMemberAsync` — after inviting a user
- `SetMemberRoleAsync` — after changing access level
- `SetMemberTenantRolesAsync` — after changing tenant roles
- `SetMemberScopeOverridesAsync` — after changing scope overrides
- `SetInvitationResponseAsync` (reject path) — after rejecting an invitation
- `SetTeamConsentAsync` — after toggling consent

### Part B: Leave team with validation

- Regular users can leave any team
- Admins can leave if at least one other admin (or owner) remains
- Owners can leave only after transferring ownership to another member
- Add "Transfer Ownership" UI (dialog or dropdown) before owner can leave
- Validation in `TeamServiceBase` to prevent leaving when rules are violated

## Acceptance criteria

- [ ] All six methods fire `TeamsListChangedEvent`
- [ ] Tests verify event firing for each method
- [ ] Regular users can leave a team
- [ ] Admins cannot leave if they are the last admin/owner
- [ ] Owners must transfer ownership before leaving
- [ ] Transfer ownership UI works in TeamComponent
- [ ] All existing tests pass

## Done condition

All acceptance criteria met, all tests pass, user confirms feature is complete.

# Feature: Team Creation Improvements (Requests #3, #9)

## Goal
Improve default team name resolution and allow passing a display name for the team owner.

## Scope

### 1. Use GetEmail() for default team name (#3)
- `TeamServiceBase.CreateTeamAsync` should use `ClaimsExtensionsStandard.GetEmail()` as fallback for resolving the username used in the default team name
- From: Eplicta.FortDocs — Priority: Medium

### 2. Accept optional display name in CreateTeam (#9)
- Add `string? displayName` parameter to `CreateTeamAsync` (or derive from claims)
- Set the owner member's `Name` from this value
- From: Eplicta.FortDocs — Priority: Low

## Acceptance Criteria
- [ ] Default team name uses GetEmail() fallback when email claim is missing
- [ ] Owner member has a display name set when creating a team
- [ ] Backward-compatible — existing overrides still work
- [ ] Tests cover both scenarios

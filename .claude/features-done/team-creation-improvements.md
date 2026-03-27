# Feature: Team Creation Improvements (Requests #3, #9)

## Originating branch
develop

## Goal
Improve default team name resolution and pass display name to team owner.

## Approach
1. Add `ResolveDisplayName(IUser)` helper in TeamServiceBase that extracts a display name from email
2. Use it for the default team name (fallback chain: name → email username → "Unknown")
3. Pass display name to `CreateTeamAsync(teamKey, name, user, displayName)` so consumers can set it on the owner member

## Acceptance Criteria
- [ ] Default team name uses robust email parsing
- [ ] Display name passed to abstract CreateTeamAsync
- [ ] Backward-compatible
- [ ] Tests cover scenarios

## Done Condition
All acceptance criteria met.

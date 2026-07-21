# Feature: Highlight the current user in the team member list

## Goal

In `TeamComponent`'s member grid (the `/team` page), make it obvious at a glance which row is **you** —
so a member scanning a team doesn't have to match their own email against the list.

## Source

Backlog (`Toolkit/Platform.md` → Features): "a visual cue under the team pages so I can see who is 'me'
in the list of users."

## Scope

`Tharga.Team.Blazor/Features/Team/TeamComponent.razor` only:

- A **row tint** on the member whose `Key` matches the current user, via the grid's `RowRender`
  callback and a theme-aware background token (must work in light and dark).
- A small **"You" chip** next to that member's name in the read-only name display.
- A tiny pure helper (`MemberHighlight.IsCurrentMember`) for the "is this me?" decision, unit-tested —
  the project has no bUnit, so the razor itself is verified by build + the user's manual pass, matching
  the `TeamActionGate` / `TeamVisibility` precedent.

The current-user identity is already resolved as `_user.Key` and compared as `context.Key == _user.Key`
in several places in this component, so no new data is needed.

## Non-goals

- Highlighting in `UsersListView` / `TeamsListView` (the developer admin surfaces) — "me" is far less
  meaningful when an admin is viewing all users/teams. Team member list only.
- Any new component parameter or configuration — this is an unconditional visual improvement, on by
  default, no opt-in.

## Acceptance criteria

- [ ] The current user's row in the member grid is visibly tinted (light and dark theme).
- [ ] A "You" chip appears next to the current user's name.
- [ ] Other rows are unchanged; a member with a null `Key` never matches.
- [ ] The self-detection is a pure, unit-tested function.
- [ ] Full suite green; `dotnet build -c Release` clean.
- [ ] No new public API. README touched only if the member-list description warrants a line.

## Done condition

Acceptance criteria met, user has tested from the running sample and confirmed, then close-out per the
Feature Workflow.

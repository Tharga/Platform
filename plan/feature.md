# Feature: Per-team action gating in TeamComponent (#125)

## Goal

Make `TeamComponent`'s per-team action buttons reflect what the server actually authorizes for
**that** team, so no button is offered that throws `UnauthorizedAccessException` when clicked.

## Problem

`team:manage` is emitted only for the **currently-selected** team (`TeamServerClaimsTransformation`
reads the `selected_team_id` cookie), but `TeamComponent` computes `_canManage` once in
`OnInitializedAsync` and applies that single global bool to every team in the `@foreach`. Three
distinct symptoms:

1. **Rename / Delete leak** — shown on every team card; clicking a non-selected team hits
   `TeamAuthorizer.HasTeamScopeAsync`, where `callerTeam != team.Key` → throw.
2. **Leave shows for non-members** — `HasAccessLevel` returns `false` for a non-member, so
   `Visible="@(!HasAccessLevel(team, Owner))"` is `true` for people not on the team at all.
3. **Consent selector hidden, not disabled** — a non-admin cannot see the consented level at all;
   there is no read-only state.

Reported from Eplicta.FortDocs.Web (`/team` renders a bare `<TeamComponent>`), 2026-07-18, against
Tharga.Team.* 3.1.7. Not fixable host-side — no parameter surface controls these gates.

## Scope

All in `Tharga.Team.Blazor`:

- New pure, testable gate helpers (`Framework/TeamActionGate.cs`) — mirrors the
  `CreateTeamActionResolver` / `LoginDisplay.ShouldShowTeamMenuItem` testable-static pattern, since
  the test project has no bUnit and razor markup cannot be asserted directly.
- `Features/Team/TeamComponent.razor` — four gate changes (Consent, Rename, Delete, Leave) plus two
  private helpers delegating to `TeamActionGate`.
- Unit tests covering every gate's truth table.

**Out of scope:** the optional try/catch hardening of the `DeleteTeam` / `SetConsent` /
`RemoveUserFromTeam` handlers, and surfacing the system `teams:delete` scope in the UI. Both are
noted in the issue as optional; neither is needed to stop the reported crash. Record as follow-ups.

## Acceptance criteria

- [ ] Rename is visible only when the user holds `team:manage` **and** the team is the selected one.
- [ ] Delete adds that same selected-team gate on top of the existing `AllowTeamCreation` + Owner checks.
- [ ] Leave is visible only to actual members (`MembershipState.Member`) who are not the Owner.
- [ ] Consent selector renders whenever the toggle is on, **disabled** for non-Administrators.
- [ ] Transfer Ownership behaviour is unchanged.
- [ ] Gate logic lives in pure functions with unit tests covering each truth table.
- [ ] Full test suite green; `dotnet build -c Release` clean.
- [ ] `Tharga.Team.Blazor/README.md` + `docs/articles/` reviewed for anything the change contradicts.

## Done condition

All acceptance criteria met, user has tested from the pushed branch and confirmed, then close-out
per the Feature Workflow (archive plan, remove `plan/`, final commit, PR).

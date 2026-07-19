# Plan: Per-team action gating in TeamComponent (#125)

Branch `feature/per-team-action-gating` off `master`. See `feature.md` for goal and acceptance criteria.

Scope confirmed with the user: **four gate fixes only**. The issue's optional hardening (try/catch on
the `DeleteTeam`/`SetConsent`/`RemoveUserFromTeam` handlers, and surfacing Delete for holders of the
system `teams:delete` scope) is deliberately excluded — recorded as follow-ups at the bottom.

## Steps

- [x] **1. NuGet updates (up front, whole solution)**
  All 10 applied via `dotnet outdated -u`: `Extensions.DependencyInjection(.Abstractions)` 10.0.9→10.0.10,
  `Components.Authorization` (9.0.18 / 10.0.10), `AspNetCore.OpenApi` (9.0.18 / 10.0.10),
  `NET.Test.Sdk` 18.7.0→18.8.1, `NSubstitute` 5.3.0→6.0.0.
  Release build clean (9 pre-existing warnings, 0 errors); suite **559 passed, 0 failed**.
  **The NSubstitute major bump required no code changes** — no breaking usage in the three consuming
  test projects. Committed as `chore:` separately from the feature diff.

- [x] **2. Gate tests written first** (`Tharga.Team.Blazor.Tests/TeamActionGateTests.cs`)
  22 tests: `[Theory]` truth tables for `CanManage` / `CanRename` / `CanDelete` / `CanLeave` /
  `CanEditConsent`, including null and empty team-key cases (never authorize on a null "match") and
  an ordinal case-sensitivity check on the team key.

- [x] **3. Implement `TeamActionGate`**
  Placed in `Features/Team/` — **not** `Framework/` as originally drafted — to match
  `CreateTeamActionResolver` and the "keep feature-specific code under its feature" rule. `internal`
  static, no DI dependency, reached by tests via the existing `InternalsVisibleTo`. XML docs explain
  *why* the selected-team check exists (the scope is only issued for the selected team).

- [x] **4. Wire the gates into `TeamComponent.razor`**
  Four private helpers (`CanRenameTeam`, `CanDeleteTeam`, `CanLeaveTeam`, `CanEditConsent`) plus
  `IsMemberOf`, delegating to `TeamActionGate`. Markup:
  - Consent — `@if (_showConsentToggle)` only; dropdown gains `Disabled="@(!CanEditConsent(team))"`.
  - Rename — `Visible="@(CanRenameTeam(team))"`.
  - Delete — `Visible="@(CanDeleteTeam(team))"`.
  - Leave — `Visible="@(CanLeaveTeam(team))"`.
  - Transfer Ownership — unchanged.

- [x] **5. Build + full test suite**
  Release build clean (8 warnings, all pre-existing, 0 errors); suite **581 passed, 0 failed** (+22).

- [~] **6. Docs review**
  `Tharga.Team.Blazor/README.md` — no statement about per-team action visibility; nothing contradicted.
  `docs/articles/implementation-guide.md` — the authorization tables (`:803-823`) already document the
  server rule this change makes the UI honour ("team scopes authorize only the caller's **own** team"),
  so nothing there is wrong. **One genuine addition needed:** the consent selector's
  hidden → visible-but-disabled change is consumer-visible and belongs in the Consent section.
  Land as a separate `docs:` commit.

- [ ] **7. Push branch, hand to user for testing**
  Do **not** open the PR yet (close-out commit must land last).

- [ ] **8. Close-out** — only after the user confirms
  Re-run `dotnet outdated`, archive `feature.md` to the Plan directory `done/`, `git rm -r plan`,
  final commit `fix: per-team-action-gating complete`, push, open PR.

## Notes / decisions

- **Why a separate `TeamActionGate` rather than helpers in the razor:** the Blazor test project has
  no bUnit, so logic embedded in markup or in `@code` private methods is unreachable from tests. The
  `create-team-override` feature set the precedent (`CreateTeamActionResolver`).
- **Consent stays visible-but-disabled** per the issue's desired behaviour #1 — a viewer should see
  the consented level without being able to change it.
- **`CanEditConsent` is currently an identity function.** Kept as a named gate anyway so the
  visible-vs-enabled split is expressed and tested in one place rather than inline in markup.

## Follow-ups (deliberately out of scope)

- Wrap `DeleteTeam` / `SetConsent` / `RemoveUserFromTeam` in try/catch so any residual authorization
  denial surfaces as a notification rather than the Blazor error UI (mirrors `TeamDialog.OnSubmit`).
- Surface Delete for holders of the system `teams:delete` scope — the new gate hides it from
  cross-team admins who have no `team:manage` on the team in question. Needs a system-scope check in
  the component. Only matters once a consumer actually uses `teams:delete` from the UI.

## Last session

**2026-07-19.** Branch created off `master`; plan confirmed with the user (four gate fixes only).
Steps 1–5 complete: packages updated (NSubstitute 6.0 clean), `TeamActionGate` + 22 tests added,
four markup gates wired, suite green at 581. Step 6 (docs) in progress — one consent-behaviour
addition to the implementation guide, then push for user testing (step 7).

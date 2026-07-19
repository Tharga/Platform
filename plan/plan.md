# Plan: Per-team action gating in TeamComponent (#125)

Branch `feature/per-team-action-gating` off `master`. See `feature.md` for goal and acceptance criteria.

## Steps

- [ ] **1. NuGet updates (up front, whole solution)**
  Run `dotnet outdated -u` across the solution and verify build + full test suite **before** any
  feature code. 10 updates pending:
  - `Microsoft.Extensions.DependencyInjection` / `.Abstractions` 10.0.9 → 10.0.10
  - `Microsoft.AspNetCore.Components.Authorization` 9.0.17 → 9.0.18 (net9), 10.0.9 → 10.0.10 (net10)
  - `Microsoft.AspNetCore.OpenApi` 9.0.17 → 9.0.18 (net9), 10.0.9 → 10.0.10 (net10)
  - `Microsoft.NET.Test.Sdk` 18.7.0 → 18.8.1
  - **`NSubstitute` 5.3.0 → 6.0.0 — MAJOR**, used by `Tharga.Platform.Mcp.Tests`,
    `Tharga.Team.MongoDB.Tests`, `Tharga.Team.Service.Tests` (the Blazor test project uses Moq).
    Breaking changes possible; if the suite breaks, fix forward on this branch.
  Commit separately (`chore:`) so the feature diff stays clean.

- [ ] **2. Write the gate tests first** (`Tharga.Team.Blazor.Tests/TeamActionGateTests.cs`)
  `[Theory]` truth tables for each gate, written against the not-yet-existing `TeamActionGate`:
  - `CanManage(hasManageScope, selectedTeamKey, teamKey)` — true only when scope held **and** keys match.
  - `CanRename` — same as `CanManage`.
  - `CanDelete(hasManageScope, selectedTeamKey, teamKey, allowTeamCreation, isOwner)` — all four must hold.
  - `CanLeave(isMember, isOwner)` — member and not owner.
  - `CanEditConsent(isAdministrator)` — visibility vs. enablement kept separate.
  Include null/empty team-key cases (never authorize on a null match).

- [ ] **3. Implement `TeamActionGate`** (`Tharga.Team.Blazor/Framework/TeamActionGate.cs`)
  Pure static functions, no component/DI dependency. XML doc comments on the public surface
  explaining *why* the selected-team check exists (claims are per-selected-team). Tests from step 2 pass.

- [ ] **4. Wire the gates into `TeamComponent.razor`**
  Add private `CanManageTeam(team)` / `IsMemberOf(team)` helpers delegating to `TeamActionGate`
  (`_selectedTeam?.Key`, `_user.Key`, `MembershipState.Member`), then the four markup changes:
  - `:35` Consent — drop `HasAccessLevel(...)` from the `@if`, add
    `Disabled="@(!HasAccessLevel(team, AccessLevel.Administrator))"` on the dropdown.
  - `:47` Rename — `Visible="@(CanManageTeam(team))"`.
  - `:48` Delete — `Visible="@(CanManageTeam(team) && _allowTeamCreation && HasAccessLevel(team, AccessLevel.Owner))"`.
  - `:50` Leave — `Visible="@(IsMemberOf(team) && !HasAccessLevel(team, AccessLevel.Owner))"`.
  - `:49` Transfer Ownership — unchanged.

- [ ] **5. Build + full test suite**
  `dotnet build -c Release` then `dotnet test -c Release`. No failing tests before commit.

- [ ] **6. Docs review**
  Check `Tharga.Team.Blazor/README.md` and `docs/articles/implementation-guide.md` for statements
  about who sees which team action. This is a bug fix, so new content is likely unnecessary — but
  the review is mandatory, and the outcome gets stated either way. Land as a separate `docs:` commit
  if anything changes.

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

## Last session

Not yet started — plan awaiting confirmation.

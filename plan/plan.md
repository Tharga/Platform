# Plan: Cross-team visibility for oversight roles

Branch `feature/cross-team-visibility` off `master` (at `9117f3c`). See `feature.md` for the model,
scope and acceptance criteria.

## Steps

- [x] **1. NuGet updates (up front, whole solution)** — no-op this time. `dotnet outdated` reports
  "No outdated dependencies were detected" (the previous feature applied all 10 an hour earlier).
  Re-check at close-out per the workflow.

- [ ] **2. `teams:read` scope constant + opt-in registration**
  - Add `SystemTeamScopes.Read = "teams:read"` with XML docs contrasting it against the in-team
    `TeamScopes.Read`.
  - Add `ConsentOptions.GrantTeamsRead` (bool, default `false`). When true, registration maps every
    role in `Consent.Roles` to `teams:read`, **merged with** any existing `ConfigureSystemRoles`
    mapping rather than replacing it.
  - Tests: default-off changes nothing; on, each consent role gains the scope; a role already mapped
    isn't duplicated; empty/null `Consent.Roles` is safe.

- [ ] **3. Service layer — the widened read path**
  Non-breaking shape matters here; consumers derive from `TeamServiceRepositoryBase` (confirmed in
  the sample) and could implement `ITeamRepository<,>` themselves.
  - `ITeamService.GetAllTeamsAsync()` + generic `GetAllTeamsAsync<TMember>()`.
  - `TeamServiceBase`: **`virtual`**, delegating to a `protected virtual GetAllTeamsInternalAsync()`
    that returns empty by default. *Not abstract* — an abstract member would fail to compile every
    consumer's derived `TeamService`.
  - `TeamServiceRepositoryBase`: override it onto the repository.
  - `ITeamRepository<,>.GetAllTeamsAsync()` as a **default interface method that throws
    `NotSupportedException`** with a message naming the fix. Compile-safe for custom repositories,
    loud rather than silently-empty if one is used with the feature enabled.
  - `TeamRepository`: real implementation.
  - `AuthorizationTeamServiceDecorator`: gate on `teams:read`, mirroring the `teams:delete` pattern.
  - `AuditingTeamServiceDecorator`: plain pass-through (no audit — user decision).
  - Tests: decorator allows with the scope, denies without; base default returns empty.

- [ ] **4. Team-list resolution helper**
  One pure function — "holder of `teams:read` → all teams, otherwise own teams" — so the three
  listing surfaces share one tested decision instead of repeating a claims check. Follows the
  `TeamActionGate` / `CreateTeamActionResolver` precedent (no bUnit in this project).

- [ ] **5. Wire the listing surfaces**
  `TeamComponent.ReloadTeams()`, `TeamsListView` and `TeamSelector` call the helper.
  Verify the `_selectedTeam` logic in `TeamComponent` still holds when the selected team is one the
  user isn't a member of.

- [ ] **6. Consent badge / indicator**
  - `RadzenBadge` with `Variant.Flat`, text = level name, `BadgeStyle` = `Danger` (no access) /
    `Warning` (Viewer, User) / `Success` (Administrator). Theme-aware by construction; no hard-coded
    light colours (they break dark theme).
  - Shown on `TeamComponent` cards and as a `TeamsListView` column; small tinted dot on
    `TeamSelector` where a full badge won't fit.
  - Gated on holding `teams:read` — **not** on a role-name string.
  - Level→style/label mapping lives in the step-4 helper class so it is unit-tested.

- [ ] **7. Non-member selection behaviour**
  Confirm the existing consent branch fires for a `teams:read` holder selecting a non-member team,
  and that a team with **no** consent degrades cleanly (team visible, no team scopes, no exception
  and no blank page). This is verification of existing code, plus any guard the check turns up.

- [ ] **8. Build + full test suite**
  `dotnet build -c Release`, `dotnet test -c Release`. No failing tests before any commit.

- [ ] **9. Docs**
  `docs/articles/implementation-guide.md` — add `teams:read` to the system-scope table, document
  `GrantTeamsRead` in the Consent section, and explain the discovery-vs-access split (this is the
  concept most likely to be misread). `Tharga.Team.Blazor/README.md` — the new option and the badge.
  Separate `docs:` commit.

- [ ] **9b. Version line** — set `MAJOR_MINOR: '3.2'` in `.github/workflows/build.yml`.

- [ ] **10. Push branch, hand to user for testing** — do **not** open the PR yet.

- [ ] **11. Close-out** — only after the user confirms. Re-run `dotnet outdated`, archive
  `feature.md` to the Plan directory `done/`, `git rm -r plan`, final commit
  `feat: cross-team-visibility complete`, push, open PR.

## Decisions taken

- **Explicit opt-in flag** over auto-deriving `teams:read` from `Consent.Roles` (user choice).
  Auto-deriving would silently widen privileges for existing hosts on upgrade.
- **Badge with text + tint**, not colour alone and not an icon alone. Colour-only fails for
  colour-blind users and in dark theme; an icon alone needs a legend nobody reads.
- **Gate the UI on the scope, not a role name** — role names are host-configurable, and
  `ApiKeyManagementService.cs:15` already shows the cost of hard-coding `"Developer"`.
- **No audit on enumeration** (user decision). Mutations stay audited via the existing decorator.

## Decisions confirmed with the user (2026-07-19)

1. **Colour story: green = full access.** `Danger` = no access, `Warning` = Viewer/User,
   `Success` = Administrator. Reads as "how much can I do here" — the support-user framing.
2. **`TeamSelector` dot is in scope** — the selector is where an oversight user scans teams most
   often, so it would be the odd gap.
3. **Bump the minor line to 3.2** — this adds public API surface, so a patch would understate it.
   Requires `MAJOR_MINOR: '3.1'` → `'3.2'` in `.github/workflows/build.yml` (step 9b below).
   Note: 3.1 still carries two *unreleased* fixes (#123, #125). Bumping to 3.2 means those ship on
   the 3.1 line only if the gated release is approved **before** this merges; otherwise all three
   land together as 3.2.0. Flag to the user at close-out.

## Last session

**2026-07-19.** Branch created off `master`; `dotnet outdated` clean so step 1 was a no-op. Plan
written and awaiting confirmation — no code written yet.

# Plan: Cross-team visibility for oversight roles

Branch `feature/cross-team-visibility` off `master` (at `9117f3c`). See `feature.md` for the model,
scope and acceptance criteria.

## Steps

- [x] **1. NuGet updates (up front, whole solution)** — no-op this time. `dotnet outdated` reports
  "No outdated dependencies were detected" (the previous feature applied all 10 an hour earlier).
  Re-check at close-out per the workflow.

- [x] **2. `teams:read` scope constant + opt-in registration** — done. Suite 595 green (+14).
  **Finding worth keeping:** `SystemRoleRegistry.Map` *throws* on an already-mapped role, so composing
  the grant onto a consumer's existing `ConfigureSystemRoles` mapping would have crashed at startup —
  in exactly the most likely configuration (host maps `Developer`, consent roles default to
  `["Developer"]`). Added `SystemRoleRegistry.Grant`, a merge-capable sibling that unions scopes and
  never throws; `Map` stays strict so duplicate *consumer* config is still reported. `Grant` went on
  the concrete class only — `ConfigureSystemRoles` is `Action<SystemRoleRegistry>`, so
  `ISystemRoleRegistry` did not need to change and no implementer breaks.

  Original step text:
  - Add `SystemTeamScopes.Read = "teams:read"` with XML docs contrasting it against the in-team
    `TeamScopes.Read`.
  - Add `ConsentOptions.GrantTeamsRead` (bool, default `false`). When true, registration maps every
    role in `Consent.Roles` to `teams:read`, **merged with** any existing `ConfigureSystemRoles`
    mapping rather than replacing it.
  - Tests: default-off changes nothing; on, each consent role gains the scope; a role already mapped
    isn't duplicated; empty/null `Consent.Roles` is safe.

- [x] **3. Service layer — the widened read path** — done. Suite **601 green** (+6). All non-breaking
  shapes landed as planned: `TeamServiceBase.GetAllTeamsInternalAsync` is `virtual` (the pre-existing
  `TestTeamService` compiles untouched and yields empty — asserted as a test), `ITeamRepository<,>`
  got a throwing default interface method, `ISystemRoleRegistry` was not touched.
  **Snag:** `AsyncEnumerable.Empty<T>()` resolves on net10 but **not** net9, and `Tharga.Team`
  multi-targets both — replaced with a plain `await Task.CompletedTask; yield break;` iterator, which
  is framework-independent and warning-free.

  Original step text:
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

- [x] **4. Team-list resolution helper** — done as `Features/Team/TeamVisibility.cs` (+14 tests):
  `CanSeeAllTeams(principal)` keyed on the **scope**, and `Resolve`/`Label`/`BadgeStyle` reducing a
  team's consent to the three UI states. `BadgeStyle` returns a string so the helper stays free of a
  Radzen dependency and unit-testable. Note the enum is `internal`, so tests compare `.ToString()` —
  same workaround `CreateTeamOverrideTests` uses (an internal type can't sit in a public test signature).

  Original step text:
  One pure function — "holder of `teams:read` → all teams, otherwise own teams" — so the three
  listing surfaces share one tested decision instead of repeating a claims check. Follows the
  `TeamActionGate` / `CreateTeamActionResolver` precedent (no bUnit in this project).

- [x] **5. Wire the listing surfaces + fix selection for non-member teams** — done. Suite 615 green.
  `TeamStateService` now resolves two sets: `teams` (own) drives every automatic choice,
  `visibleTeams` (widened when the caller holds `teams:read`) only validates an existing selection.
  `AutoCreateFirstTeam` therefore behaves exactly as before — **no silent behaviour change**.
  `GetVisibleTeamsAsync` falls back to own teams on `UnauthorizedAccessException` so a claims/enforcement
  mismatch degrades instead of breaking the page. `AssignTeamAsync` null-guarded.
  `SetSelectedTeamAsync` remembers every explicit selection, member or not (**revised by the user
  2026-07-19** — originally cookie-only/session-scoped). Restoring it required more than persisting:
  the restore path looked the remembered key up in *own* teams, so a non-member team would never have
  resolved. Resolution is now uniform — current cookie → remembered key → own-teams fallback — via
  `TeamSelectionResolver`, replacing the old branch-per-case structure. The security guard is unchanged
  in substance: a *chosen* team is honoured, a team never chosen is never defaulted to.
  `TeamComponent:29` now gates on `_teams.Any()` rather than `_selectedTeam != null`.
  All three surfaces (`TeamComponent`, `TeamSelector`, `TeamsListView`) pick the widened list.

- [x] **6. Consent badge / indicator** — done alongside step 5.
  `TeamComponent`: consent badge + a "Not a member" badge, both only for `teams:read` holders.
  `TeamSelector`: tinted dot with the level as a tooltip. `TeamsListView`: a Consent column.
  `ConsentVisibility` had to become **public** — `TeamViewModel` is public API (consumers template
  against it via `TeamActionsTemplate`), so an internal enum could not sit on it. `TeamVisibility`
  itself stays internal.

  Original step text for 5–6:
  Scope grew after tracing the selection path (2026-07-19). Listing alone is not enough:
  - `TeamComponent.ReloadTeams()`, `TeamsListView`, `TeamSelector` call the step-4 helper.
  - **`TeamStateService.GetSelectedTeamAsync` must use the widened set too.** It currently reads the
    membership-scoped list (`:46`), finds the selected non-member team absent (`:48`), and falls back
    to `teams.FirstOrDefault()` (`:53`) — silently reverting the selection. Without this the feature
    does not work at all.
  - **`TeamComponent:29` gates the whole `@foreach` on `_selectedTeam != null`.** A `teams:read`
    holder who belongs to *no* team — the core persona — would see an empty page. Render the list
    when there are teams, whether or not one is selected.
  - **Null-guard `AssignTeamAsync`** — it dereferences `_selectedTeam.Key` under `refresh: true`
    (`:88`). Unreachable today; reachable once the team set widens.

  **Hard requirement (user, 2026-07-19): Platform must not throw** when a `teams:read` holder selects
  a team that is neither theirs nor consented. Verified safe already: `TeamServerClaimsTransformation`
  adds no claims and returns the principal (`:79-106`), and `SetMemberLastSeenAsync` is an ungated
  pass-through whose repository call `PickOneOrDefault`s to null and early-returns.

- [ ] **6. Consent badge / indicator**
  - `RadzenBadge` with `Variant.Flat`, text = level name, `BadgeStyle` = `Danger` (no access) /
    `Warning` (Viewer, User) / `Success` (Administrator). Theme-aware by construction; no hard-coded
    light colours (they break dark theme).
  - Shown on `TeamComponent` cards and as a `TeamsListView` column; small tinted dot on
    `TeamSelector` where a full badge won't fit.
  - Gated on holding `teams:read` — **not** on a role-name string.
  - Level→style/label mapping lives in the step-4 helper class so it is unit-tested.

- [x] **7. Non-member selection behaviour** — done. `TeamStateService` is internal with heavy Blazor
  dependencies (`NavigationManager`, `IJSRuntime`, `ILocalStorageService`) and the project has no bUnit
  or any precedent for testing it, so the security-critical decision was extracted to a pure
  `TeamSelectionResolver` and tested directly (9 tests) rather than left on reasoning. The guard it
  encodes: an explicit selection of a visible team is honoured, but the **fallback always comes from
  own memberships** — including the "scope but no memberships" case, which resolves to *nothing*
  rather than the first tenant in the list.
  Graceful-degradation verified by inspection earlier: claims transformation adds nothing and returns
  the principal; `SetMemberLastSeenAsync` is an ungated pass-through that early-returns for non-members.

- [x] **8. Build + full test suite** — Release build clean; suite **624 passed, 0 failed** (was 559 at
  branch start; +65).

- [x] **9. Docs** — `implementation-guide.md`: `teams:read` in the system-scope table plus a
  "Cross-team visibility for oversight roles" section covering the discovery-vs-access split, the
  opt-in flag and why it defaults off, the badges, and the session-scoped selection rule.
  `Tharga.Team.Blazor/README.md`: matching section.

- [x] **9b. Version line** — `MAJOR_MINOR: '3.2'`.

  Original step text for 7:
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

- [ ] **11b. Correct the `Requests.md` follow-up wording.** The #123 / #125 follow-up rows say "next
  release on the 3.1 line". User decided 2026-07-19 that all three ship together as **3.2.0** and
  there will be no 3.1.8 — update those rows to name 3.2.0.

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

## Follow-ups filed

- **[#127](https://github.com/Tharga/Platform/issues/127) — team claims go stale for the life of a
  Blazor Server circuit.** Filed 2026-07-19. Pre-existing, not caused by this feature, but this feature
  widened who it affects. Consent revocation is the *weakest* case (a team switch forces a full reload,
  so claims refresh); member removal and access downgrade are stronger, having no natural refresh point.
  Also affects the service layer, not just the UI — `BlazorTeamPrincipalAccessor` falls back to the
  circuit's frozen authentication state. Suggested fix is a `RevalidatingServerAuthenticationStateProvider`
  on a configurable interval; deliberately **not** folded in here, since it changes authentication
  behaviour for every consumer and needs its own testing pass.
  Current behaviour is now documented in `implementation-guide.md` (claims-enricher section).

## Last session

**2026-07-19.** Branch created off `master`; `dotnet outdated` clean so step 1 was a no-op. Plan
written and awaiting confirmation — no code written yet.

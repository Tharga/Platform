# Plan: Oversight defects (#149, #150, #151)

Branch: `feature/oversight-defects` (from `master`)

## Steps

- [x] **1. NuGet package check (up front)** — `dotnet outdated` across the solution. Only
      `SixLabors.ImageSharp 3.1.12 -> 4.0.0` is outstanding, and it is **held deliberately**: 4.0+ requires a
      paid Six Labors build-time licence. Nothing to apply; no upgrade commit for this feature.

- [x] **2. Baseline** — `dotnet build -c Release` clean (0 errors, 10 pre-existing warnings);
      `dotnet test -c Release` **985 passing** across 6 projects (Entra 17, Images 4, MongoDB 39, Mcp 51,
      Service 490, Blazor 384). Any later failure is attributable to this feature.

- [x] **3. #149 — distinguish "no team selected" from "access denied"** — done. 4 tests added to
      `AccessLevelProxyTests` (494 total in that project, up from 490); the two defect tests were verified to
      **fail against the unfixed code** before the fix was restored.

  **The issue's suggested fix was not implementable as written.** It names
  `Constants.TeamKeyCookie`, which lives in `Tharga.Team.Blazor.Framework` and is `internal` — but the fix
  belongs in `Tharga.Team.Service`, and the dependency runs `Blazor -> Service -> Tharga.Team`. Service
  cannot see it. Resolved by putting the marker where both packages can reach it:
  - Added **`TeamClaimTypes.SelectedTeamKey`** (`"team_id"`) in `Tharga.Team`, documented as the *selection
    marker* against `TeamKey`'s *access anchor*, with the distinction spelled out on both members.
  - `AccessLevelProxy.CheckAccessLevel` now reports `"Access denied for the selected team '<key>'."` when the
    marker is present, and keeps `"No team selected."` when it is not.
  - Pointed `Constants.TeamKeyCookie` at the new constant so `"team_id"` has **one** definition rather than
    two that could drift. Left the (misleading) `Cookie` name alone — it is internal, purely a claim type,
    and renaming it touches 8 files for no defect-fixing reason.

  **Sibling check — neither is affected, decided explicitly:**
  - `ScopeProxy` resolves the team from the *call's own arguments* (`ResolveTeamKey`), not from the claim, so
    "which team" is never ambiguous and its message is accurate.
  - `Tharga.Platform.Mcp/PlatformTeamResourceProvider.cs:82` throws the same string, but its `context.TeamId`
    comes from an API-key principal. The selection marker is a Blazor cookie-derived claim that never exists
    on that path, so "No team selected." is the truth there.

- [x] **4. #150 — oversight team counts in `UsersListView`** — done. `_canSeeAllTeams` resolved from the
      auth state already being fetched in `OnInitializedAsync`, and `LoadDataAsync` now branches exactly as
      `TeamsListView:118-120` does. Non-oversight path byte-for-byte unchanged. Needed
      `@using Tharga.Team.Blazor.Features.Team` (not in `_Imports.razor`).

  **Authorization verified, not assumed.** `AuthorizationTeamServiceDecorator.GetAllTeamsAsync<TMember>()`
  calls `RequireAllTeamsReadAsync()` before enumerating, so `teams:read` is enforced **server-side in the
  domain**. `TeamVisibility.CanSeeAllTeams` is therefore purely a display gate choosing which call to make —
  the UI adds no second enforcement point, and target rule 2 holds. No authorization boundary moved.

  **Not unit-tested, and why.** The decision function `TeamVisibility.CanSeeAllTeams` is already covered by 4
  tests in `TeamVisibilityTests`; the fix is wiring that decision into a second component. The razor line
  itself is unreachable without bUnit, which this project does not have — the same reason its sibling in
  `TeamsListView` has no test either. A test over the extracted lookup would not have caught this defect:
  the lookup was always correct, its *input* was too narrow. Saying so beats manufacturing coverage that
  proves nothing.

- [x] **5. #151 — directory-only grid refresh** — done. Chose **fresh-list reassignment** (the
      `UsersListView` precedent) over `@ref` + `Reload()` (`TenantRoleManager`): the load is a stream, so
      `Reload()` would have to be called from inside the `await foreach` *and* after it, and it needs a null
      guard on every call. Reassignment makes the grid correct by construction — there is no in-place
      mutation left to forget to announce.
  - Accumulates into a local `loaded`, publishing `_users = [.. loaded]` on each 25-item batch and once more
    when the stream completes. The final assignment is what fixes the <25 case; the batching path is kept
    (it gives progressive rendering on large directories) and now actually works, since it changes the
    reference the grid is keyed on.
  - `_users` changed from `readonly` to reassignable, with a one-line comment recording *why* — the
    reassignment reads as gratuitous otherwise, which is how the defect got written in the first place.
  - **Not unit-testable here**: no bUnit, and a grid-refresh fix has no pure seam to extract. Verified by
    inspection; needs the user's manual check against a directory with fewer than 25 directory-only users.

- [x] **6. Full verification** — `dotnet build -c Release` 0 errors; `dotnet test -c Release` **989 passing**,
      0 failed, up exactly 4 from the 985 baseline. No pre-existing test changed behaviour.

- [ ] **7. Documentation review** — check both surfaces per the Feature Workflow: root `README.md` and the
      `docs/` site. #149 changes a user-visible error message and #150 changes what an oversight admin sees;
      check whether either is described anywhere. Land as a separate `docs:` commit, or state explicitly that
      no consumer-visible doc surface covers these.

- [ ] **8. Push the branch** for the user to test from origin. Do **not** open the PR yet, and do **not**
      close the feature — wait for the user to confirm.

## Close-out (only when the user says it is done)

- [ ] Re-run `dotnet outdated` (new releases may have published mid-feature); apply and include in this PR.
- [ ] Archive `plan/feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/oversight-defects.md`
- [ ] `git rm -r plan`
- [ ] Final commit `fix: oversight-defects complete`, push, open PR referencing #149, #150, #151

## Notes

- Also outstanding from the previous session, unrelated to this feature: the stale spec
  `$DOC_ROOT/Tharga/plans/Toolkit/Platform/planned/02-authorization-defects.md` still needs deleting — plan 02
  shipped in PR #152. The `planned/README.md` table has been updated to say so.

## Last session

Session of 2026-07-27. Preflight clean (tree clean, master level with origin, packages current). Confirmed all
three issue diagnoses against source before planning. Branch created, plan written, awaiting confirmation
before any code change.

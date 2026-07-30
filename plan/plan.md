# Plan: Access-guard ordering and the `mcp:discover` scope model

Feature scope in `feature.md`. Tests run before each commit; `plan.md` is updated as each step lands.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      *Done — `dotnet outdated` across the whole solution reports only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, deliberately held (4.0+ needs a paid Six Labors build-time licence). Nothing to
      apply.*

- [x] **2. `ApiKeyView` — tests first, then the guard order**
      *Done — `AccessGuardState.Resolve` in `Framework/` + `AccessGuard` enum, 8 tests. Markup keeps its
      if/else chain but is driven by the resolver, so the diff stays small and the long inner chain
      (`_selectedTeam == null`, `_keys == null`) is untouched.*
      Extract the branch choice into a pure resolver (e.g. `AccessGuardState.Resolve(serviceMissing,
      teamLoaded, hasAccess)` returning an enum: `NotConfigured` / `Loading` / `Denied` / `Ready`) and
      test it, including the case that defines the bug: **not loaded and not yet authorized must resolve
      to `Loading`, never `Denied`.** Then have the markup switch on it.
      Rationale for a resolver over simply swapping two lines: the defect is ordering, ordering lives in
      markup, and markup is unreachable from tests in this project. Swapping the lines fixes today and
      guards nothing.

- [x] **3. Audit sibling views for the same shape**
      *Done — one more found and fixed: **`AuditLogView`** had the identical defect (`_hasAccess` defaults
      false, checked ahead of the loading branch), so every audit dialog flashed "Access denied." —
      including the per-row dialogs shipped in 3.7.0/3.7.1 where it is the entire content. Needed a new
      `_accessResolved` flag, since it had no loaded flag to order against.
      **Cleared, with reasons:** `UsersListView` and `TeamsListView` default `_canAdminister = true`, so
      they never render a false denial (they render nothing until `_loaded` — a blank, not a wrong claim);
      `SystemApiKeyView` has no access flag at all; `UsersView` has no guard.*
      Grep the Blazor components for an access flag defaulting to `false` and assigned inside an async
      lifecycle method, rendered ahead of a loaded flag. Candidates to check explicitly: `AuditLogView`,
      `SystemApiKeyView`, `UsersListView`, `TeamsListView`, `UsersView`, `TenantRoleManager`. Fix what
      matches; record in this plan what was cleared, so the sweep is not repeated blindly later.

- [x] **4. `McpScopeChecker` — tests first, then both provenances**
      *Done — 4 new tests. Delegates to `TeamScopePolicy` rather than inspecting claims, via a new
      `InternalsVisibleTo` for `Tharga.Team.Mcp` mirroring the one `Tharga.Team.Blazor` already has. Keeps
      the public surface unchanged (still a patch) and avoids a third copy of the policy.*
      **Correction made while writing the tests:** a "scope held for a different team" case cannot arise
      here. MCP has no target-team argument — the caller's `TeamKey` claim *is* the context — so the
      binding is the pairing of a `Scope` claim with a `TeamKey` claim, and the meaningful negative is a
      scope held with no team selected. Tested as such rather than as a cross-team check that would
      always pass.
      Tests: team grant for the selected team passes; team grant for a *different* team fails; system
      grant passes with no team selected; no grant fails; null `HttpContext` fails. Then implement via
      `TeamScopeGate.HasSystemScope` / `HasTeamScope` — never a bare `HasClaim`, which is the unbound form
      plan 02 removed.

- [x] **5. Register `mcp:discover` in the system registry too**
      *Done — `AddThargaSystemScopes` alongside the existing `AddThargaScopes` in `AddTeam()`; XML doc on
      `McpScopes.Discover` now states all three routes.*
      Keep the existing `AddThargaScopes` team registration; add `AddThargaSystemScopes` alongside it in
      `McpTeamBuilderExtensions.AddTeam()` so a system API key can be granted it. Update the
      `McpScopes.Discover` XML doc to state both routes.

- [x] **6. Verify build + full suite** — *Done: 1058 passed / 0 failed across 6 projects; build clean,
      0 warnings.*

- [~] **7. Documentation**

      The MCP docs and `Tharga.Team.Mcp/README.md`: how `mcp:discover` is granted (access level, system
      role, or system API key) and that a team grant authorizes only the selected team. Separate `docs:`
      commit.

- [ ] **8. Push and hand over for testing**
      Do **not** open the PR — the close-out commit must be last.

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`; mark both requests **Done** in `Requests.md` with the `## Follow-up` entry;
archive `feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/`; `git rm -r plan`; commit
`fix: access-guard-and-mcp-scope complete`; push; open the PR.

## Notes

- **No version-line change.** Both are fixes on the 3.7 line; CI increments the patch from git tags.
- **Both defects were found by decompiling the shipped assembly, not by running the app.** That is the
  same gap bUnit addresses, and it is the argument for doing bUnit next rather than later.

## Last session

Branch created off master after 3.7.1 (PR #164) merged. `mcp:discover` model chosen by the user: grantable
by access level, by system role, and to a system API key, with the checker accepting both provenances.

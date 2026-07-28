# Plan: Delete a team from the Teams tab

Feature scope in `feature.md`. Steps run in order; tests run before each commit.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      `dotnet outdated` across the solution. Apply everything available.
      *Done — only `SixLabors.ImageSharp` 3.1.12 → 4.0.0 is available and it is deliberately held: 4.0+
      requires a paid Six Labors build-time licence. Nothing else is outdated, so nothing applied.*

- [x] **2. Tests for the gate, first**
      In `Tharga.Team.Blazor.Tests`, assert the rule before the UI exists:
      - a caller holding `SystemTeamScopes.Delete` is offered delete
      - a caller with only `SystemUserScopes.Manage` is not
      - a team-level `TeamScopes.Manage` holder is not (system scope only)
      - consent state does not affect the answer
      Extract the decision into a testable helper rather than testing markup — mirrors how
      `UserAdminGate` / `TeamVisibility` are already unit tested.
      *Done — `UserAdminGate.CanDeleteTeams` added as the named decision, with
      `TeamDeleteGateTests` (7 tests) covering all four cases plus anonymous and a combined-scopes case.
      No bUnit in the test project, so markup itself is not renderable in tests; the gate is where the
      rule lives and is tested directly.*

- [x] **3. Delete action in `TeamsListView`**
      Resolve `_canDeleteTeams` in `OnInitializedAsync` via `TeamScopeGate.HasSystemScope(user,
      SystemTeamScopes.Delete)`. Render Delete in the existing action column, alongside View and the
      `TeamActionsTemplate`. Confirm with team name + member count, then call
      `TeamService.DeleteTeamAsync<TMember>(team.Key)`, clear `_selectedTeam` if it was the deleted team,
      reload, and notify. Catch and surface failures as an error notification.
      *Done — extracted `LoadDataAsync` from `OnInitializedAsync` so the grid can reload after a delete.
      Action column widened 120px → 200px to fit the second button, matching `UsersListView`.*

- [x] **4. Verify build + full test suite**
      `dotnet build -c Release` then `dotnet test -c Release`. Commit steps 2-4 together.
      *Done — build clean (0 errors), full suite 1000 passed / 0 failed.*

- [x] **5. Sample host grant**
      In `Tharga.Team.Sample/Program.cs`, grant `Developer` and `Administrator` the `SystemTeamScopes.Read`
      and `SystemTeamScopes.Delete` system scopes through the existing `ConfigureSystemRoles`, and register
      a description for `SystemTeamScopes.Delete` (`teams:delete`) alongside the existing `Read`
      registration. This is the worked example of the non-consent grant.
      *Done — `teams:delete` added to the existing `roles.Map("Developer", …)`. No separate scope
      registration needed: `ThargaBlazorRegistration` already registers `teams:delete` by default,
      merge-safe. The sample has no `Administrator` app role (only `DeveloperRoleEnricher`), so mapping
      one would have been dead config — the Administrator case is shown in the docs instead.*

- [x] **6. Documentation**
      Both surfaces, per the feature workflow:
      - `docs/articles/user-management.md` — the Teams tab gains a delete action; which scope grants it;
        that it is deliberately independent of consent.
      - `docs/articles/implementation-guide.md` — update the `<UsersView />` row in the component table.
      - `Tharga.Team.Blazor/README.md` — the `teams:read` / `UsersView` paragraph.
      Land as a separate `docs:` commit.
      *Done — added a "Deleting teams" section to `user-management.md` (new content, following the
      one-section-per-topic pattern beside "Deleting users"), updated the `<UsersView />` row in the
      implementation guide's component table, and extended the consent/`teams:read` paragraph in the
      Blazor README. Root `README.md` needed no change — its only `UsersView` mention is about directory
      features.*

- [x] **7. Push and hand over for testing**
      Push the branch. Do **not** open the PR yet — the close-out commit (archive `feature.md`, remove
      `plan/`) must be the last commit before the PR opens.
      *Done — pushed to `origin/feature/delete-team-from-teams-tab`. PR deliberately not opened; awaiting
      the user's confirmation that the feature is done.*

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`, archive `feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/`,
`git rm -r plan`, commit `feat: delete-team-from-teams-tab complete`, push, then open the PR.

## Notes

- Enforcement is not being added or moved: `AuthorizationTeamServiceDecorator.RequireDeleteAsync` already
  authorizes this call. The UI gate only stops the surface offering an action the server would reject —
  architecture-v4 rule 2 (one enforcement point) is preserved.
- Use `ITeamService.DeleteTeamAsync<TMember>`, which `TeamsListView` can already reach through its
  injected `ITeamService` and its `TMember` type parameter. `TeamComponent` uses the non-generic
  `ITeamManagementService.DeleteTeamAsync`; that is its own path and stays untouched.

## Last session

All implementation steps done and pushed; three commits on the branch (feature, sample grant, docs).
Build clean, full suite 1000 passed / 0 failed.

Two decisions taken up front and honoured throughout: no new configuration option (hosts use the
existing `ConfigureSystemRoles`), and the grant is opt-in / default-off so no existing host gains
team-delete power on upgrade.

Discovered while grounding the work: **the server side already supported this entirely** —
`SystemTeamScopes.Delete` (`teams:delete`) was already defined, registered by default, and honoured by
`AuthorizationTeamServiceDecorator.RequireDeleteAsync`. The gap was purely that `TeamsListView` never
rendered the action and nothing granted the scope to a role. So no domain, authorization or claims
change was needed.

Next: user tests from the pushed branch. On their confirmation, run the close-out sequence above.

### Unrelated finding, worth recording

Plan 01 `team-bound-service-authorization` in the Plan directory is stale. Only §3b (the startup
registration sweep) is unimplemented — §3, §5, §6 and even phase 4's §6b `TeamAccessInterceptor` are all
in the code with tests, and §6c was resolved by plan 02's `Scope`/`SystemScope` claim split.
`planned/README.md` still presents phases 2-4 as outstanding.

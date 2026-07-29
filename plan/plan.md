# Plan: Admin views

Feature scope in `feature.md`. Tests run before each commit; `plan.md` is updated as each step lands.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      *Done — only `SixLabors.ImageSharp` 3.1.12 → 4.0.0 is available and it is deliberately held: 4.0+
      requires a paid Six Labors build-time licence. Nothing else outdated.*

- [x] **2. Team delete — gate tests**
      *Done — `UserAdminGate.CanDeleteTeams` + `TeamDeleteGateTests` (7 tests): system scope opens it,
      in-team claim of the same name does not, `users:manage` alone does not, `teams:read` alone does not.*

- [x] **3. Team delete — action in `TeamsListView`**
      *Done — confirm dialog with name + member count, `ITeamService.DeleteTeamAsync<TMember>`, reload,
      notify. Extracted `LoadDataAsync`. Action column 120px → 200px.*

- [x] **4. Verify build + full suite** — *Done: 1000 passed / 0 failed.*

- [x] **5. Sample host grant** — *Done: `teams:delete` added to `roles.Map("Developer", …)`.*

- [x] **6. Documentation for the delete action** — *Done: "Deleting teams" section in
      `user-management.md`, `<UsersView />` row in the implementation guide, Blazor README paragraph.*

- [x] **7. Push the delete work** — *Done. Branch renamed `feature/delete-team-from-teams-tab` →
      `feature/admin-views` when scope broadened; old remote branch deleted.*

---

Scope broadened here. Remaining steps cover the admin-view additions.

- [x] **8. View-model and helper layer (pure, unit-tested first)**
      *Done — `TeamActivity` (LastUsed / Owner / CountByState) + `LastSeenText`, with 13 tests.
      `Rejected` counts as neither active nor invited; a null `State` counts as active (it predates the
      field). `TeamViewModel` extended additively; `MemberCount` semantics deliberately unchanged.*
      New pure statics beside `MemberHighlight` / `TeamActionGate` / `TeamVisibility`, because the project
      has no bUnit and a decision left in markup cannot be tested:
      - `TeamActivity.LastUsed(members)` — max `LastSeen`, null when none.
      - `TeamActivity.Owner(members)` — the `AccessLevel.Owner` member, null when ownerless.
      - `TeamActivity.CountByState(members)` — active vs `MembershipState.Invited`.
      - `LastSeenText.Format(value)` — `Never` for null.
      Extend `TeamViewModel` **additively** (`Icon`, `OwnerName`, `LastUsed`, `ActiveMemberCount`,
      `InvitedCount`). `MemberCount` keeps its current total-including-invited meaning — it is public API
      asserted by `UsersViewSplitTests`, so its semantics must not shift under existing consumers.

- [x] **9. Teams grid additions**
      Avatar column (`TeamAvatar`, from the newly carried `Icon`), Owner, Last used, invited-count split,
      empty-team badge. Team key + copy in the detail panel.
      *Done — also gave `TeamAvatar` the discrete `TeamKey`/`Name`/`IconReference` parameters it lacked,
      matching `UserAvatar`; it previously accepted only a whole `ITeam`, which a view model cannot supply.
      Ownerless teams get a **No owner** badge rather than an empty cell.*

- [x] **10. Users grid additions**
      Current-user row highlight (`MemberHighlight` + `data-tharga-current-member` marker and the
      cell-level `<style>` — styling the `<tr>` does **not** work, Radzen `<td>`s paint over it). Requires
      the current user, via `IUserService.GetCurrentUserAsync()` as `TeamComponent` does. User key + copy
      in the detail panel. `Never` for null Last seen.
      *Done — reused `MemberHighlight` with a `data-tharga-current-user` marker and a cell-level `<style>`.*

- [x] **11. Audit history button (opt-in)**
      `[Parameter] ShowAuditLogButton` on both list views, defaulting off, mirroring `ApiKeyView`:
      `DialogService.OpenAsync` hosting `<AuditLogView PinnedFilter="…" />` at 90vw/85vh, resizable and
      draggable. Pin `TeamKey` for a team row; `CallerIdentity` for a user row.
      *Done — user rows pin by identity, not key: audit entries record the caller's identity. Surfaced on
      `UsersView` too, so a host sets it once for both tabs.*

- [x] **12. Cross-navigation between tabs**
      `UsersView` owns the selected tab, so the coordination lives there: a callback from each child
      carrying the key to focus, and a parameter telling the child which row to expand on render.
      *Done — `OnParametersSet` (not just `OnInitializedAsync`) applies the focus, because the sibling tab
      is already rendered when the key arrives.*

- [x] **13. Verify build + full suite, commit** — *Done: 1020 passed / 0 failed across 6 projects.*

- [x] **14. Version line**
      Bump `MAJOR_MINOR` `3.6` → `3.7` in `.github/workflows/build.yml` — new public parameters are API
      growth. Must land in this PR: a merge to master queues a gated release, and the version line is a
      hand-maintained constant nothing in CI will bump.
      *Done — `3.6` → `3.7` in `build.yml`.*

- [x] **15. Documentation**
      Extend the `user-management.md` work already done, the `<UsersView />` row in the implementation
      guide, and the Blazor README with the new columns and the audit parameter. Separate `docs:` commit.
      *Done — new "Reading the admin grids" section in `user-management.md` (columns table, the
      last-used-is-selection caveat, the audit opt-in), plus the implementation-guide row and the Blazor
      README. Sample turns `ShowAuditLogButton` on, since it configures MongoDB audit storage.*

- [~] **16. Push and hand over for testing**
      Do **not** open the PR — the close-out commit must be last.

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`, archive `feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/`,
`git rm -r plan`, commit `feat: admin-views complete`, push, then open the PR.

## Notes

- Enforcement is never moved into the UI. `AuthorizationTeamServiceDecorator` already authorizes the
  delete; the gate only stops the surface offering what the server would refuse (architecture-v4 rule 2).
- No team `Created` date: `ITeam` has no such member, and adding one is an opt-in interface change of a
  different kind. Explicitly out of scope.

## Last session

Delete-a-team shipped on the branch (three commits, 1000 tests green) and pushed. Scope then broadened to
a general admin-views feature covering ten further additions across the two list views; branch renamed to
`feature/admin-views` accordingly. All additions draw on data already loaded — no new service calls.

### Unrelated finding, worth recording

Plan 01 `team-bound-service-authorization` is stale in the Plan directory: only §3b (the startup
registration sweep) is unimplemented — §3, §5, §6 and phase 4's §6b `TeamAccessInterceptor` are all in the
code with tests, and §6c was resolved by plan 02's `Scope`/`SystemScope` split. `planned/README.md` still
presents phases 2-4 as outstanding.

# Feature: users-view-split

**Originating branch:** develop
**Date started:** 2026-04-29

## Goal

Refactor `Tharga.Team.Blazor.Features.User.UsersView<TMember>` from a single 226-line component bundling Users + Teams tabs into three composable parts:

- `UsersListView` — the Users tab body (search + datagrid + selected-user drill-down)
- `TeamsListView<TMember>` — the Teams tab body (datagrid + selected-team drill-down)
- `UsersView<TMember>` — thin tabbed wrapper that composes the two (back-compat preserved)

Add a `RenderFragment<UserViewModel> ActionsTemplate` extension hook on `UsersListView` (Option A from the request) so consumers can inject row-level actions (edit, delete, custom links) without forking. The same hook lands on `TeamsListView` for symmetry — `RenderFragment<TeamViewModel>` and `RenderFragment<TeamMemberInfo>`.

Promote internal view-model records (`UserViewModel`, `UserTeamInfo`, `TeamViewModel`, `TeamMemberInfo`) to public types so consumers can type their templates against them.

## Scope

### Part A: Promote view-model records to public

- Move `UserViewModel`, `UserTeamInfo`, `TeamViewModel`, `TeamMemberInfo` out of `UsersView.razor` into separate files in `Tharga.Team.Blazor/Features/User/`
- All become `public record`

### Part B: Extract `UsersListView`

- New `Tharga.Team.Blazor/Features/User/UsersListView.razor`
- Owns the Users tab body — search box, `<RadzenDataGrid>` of `UserViewModel`, drill-down card with team-membership grid
- Resolves data the same way the current view does (via injected `IUserService` and `ITeamService`) — no parameterized data input for now (keeps scope tight; PlutusWave's use case works either way)
- Adds `[Parameter] public RenderFragment<UserViewModel> ActionsTemplate { get; set; }` — when set, renders a trailing column hosting whatever the consumer puts there per row
- **No `[Authorize(Roles = "Developer")]` attribute on the extracted child.** Authorization is the consumer's responsibility (page-level)
- Generic over `TMember` since it builds team-membership info that depends on it — same pattern as the current view

### Part C: Extract `TeamsListView<TMember>`

- New `Tharga.Team.Blazor/Features/User/TeamsListView.razor`
- Owns the Teams tab body — `<RadzenDataGrid>` of `TeamViewModel`, drill-down card with members grid
- Two row-level extension hooks:
  - `[Parameter] public RenderFragment<TeamViewModel> TeamActionsTemplate { get; set; }` — actions on a team row
  - `[Parameter] public RenderFragment<TeamMemberInfo> MemberActionsTemplate { get; set; }` — actions on a member row in the drill-down grid
- No authorization attribute (consumer responsibility)

### Part D: `UsersView<TMember>` becomes a thin wrapper

- Keep public type signature (`UsersView<TMember>`) so existing consumers (Quilt4Net.Server, PlutusWave wrapper page) don't break
- The `[Authorize(Roles = "Developer")]` attribute moves up onto the wrapper (preserves the current behavior for consumers that haven't added page-level auth)
- Renders `<RadzenTabs>` with `<UsersListView>` and `<TeamsListView>` inside
- Forwards no parameters — wrapper users get the default UX without any hooks
- Consumers that want to compose differently (e.g. only the users list with actions) drop down to the children

### Out of scope

- Built-in inline edit / delete UX (Option B in the request) — not pursued. Consumers wire their own affordances via the `ActionsTemplate`.
- Pagination/sorting redesign
- Localization
- Parameterized `IEnumerable<IUser>` input — keep service-injection-based loading

## Acceptance criteria

- [ ] `UserViewModel`, `UserTeamInfo`, `TeamViewModel`, `TeamMemberInfo` are public records in their own files
- [ ] `UsersListView.razor` renders the Users tab body and exposes an `ActionsTemplate` parameter; no `[Authorize]` attribute
- [ ] `TeamsListView<TMember>.razor` renders the Teams tab body and exposes `TeamActionsTemplate` + `MemberActionsTemplate`; no `[Authorize]` attribute
- [ ] `UsersView<TMember>.razor` becomes a tabbed composition of the two children, retains its `[Authorize(Roles = "Developer")]`
- [ ] No existing consumer needs code changes (`<UsersView TMember="..." />` keeps working unchanged)
- [ ] Test rendering each child with and without an `ActionsTemplate`
- [ ] All existing tests still pass

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete.

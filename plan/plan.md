# Plan: users-view-split

## Steps

### Part A: Promote view-model records to public
1. [x] `UserViewModel.cs` — public record
2. [x] `UserTeamInfo.cs` — public record
3. [x] `TeamViewModel.cs` — public record
4. [x] `TeamMemberInfo.cs` — public record (added `Key` field for consumer-side identity)

### Part B: Extract UsersListView
5. [x] `UsersListView.razor` — generic `<TMember>`, search + filtered grid + drill-down, `ActionsTemplate` parameter, no `[Authorize]`

### Part C: Extract TeamsListView
6. [x] `TeamsListView.razor` — generic `<TMember>`, teams grid + members drill-down, `TeamActionsTemplate` + `MemberActionsTemplate` parameters, no `[Authorize]`

### Part D: Slim down UsersView
7. [x] `UsersView.razor` is now a thin tabbed wrapper composing the two children
8. [x] `[Authorize(Roles = "Developer")]` retained on the wrapper for back-compat

### Tests
9. [x] `UsersViewSplitTests` — 6 smoke tests covering public records + parameter shapes
10. [x] Full suite: 248 tests pass

### Docs + close
11. [ ] Final commit, push, PR (back-compat: existing `<UsersView TMember="..." />` callsites unchanged)

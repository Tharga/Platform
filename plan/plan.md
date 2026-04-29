# Plan: team-member-name-edit

## Steps

### Domain (Tharga.Team)
1. [x] Add `SetMemberNameAsync(string teamKey, string userKey, string name)` to `ITeamService`
2. [x] Add the same on `ITeamManagementService` with `[RequireScope(TeamScopes.Manage)]`; pass-through impl in `TeamManagementService<TMember>`
3. [x] In `TeamServiceBase`: new `protected abstract Task SetTeamMemberNameAsync(string teamKey, string userKey, string name)`; public `SetMemberNameAsync` calls it, evicts member cache, raises `TeamsListChangedEvent`
4. [x] Add public record `MemberNameChangedArgs(string TeamKey, string MemberKey, string OldName, string NewName)` in `Tharga.Team`

### Storage (Tharga.Team.MongoDB)
5. [x] Add `Task SetMemberNameAsync(string teamKey, string userKey, string name)` to `ITeamRepository<TTeamEntity, TMember>`
6. [x] Implement in `TeamRepository<...>` using a member-targeted `UpdateOneAsync`
7. [x] Implement `SetTeamMemberNameAsync` override in `TeamServiceRepositoryBase`

### Audit (Tharga.Team.Service)
8. [x] Extend `AuditingTeamServiceDecorator` with `SetMemberNameAsync` (action: `set-member-name`)

### UI (Tharga.Team.Blazor TeamComponent)
9. [x] Add `[Parameter] public bool EnableMemberNameEdit { get; set; }` (default false)
10. [x] Add `[Parameter] public EventCallback<MemberNameChangedArgs> OnMemberNameChanged { get; set; }`
11. [x] Replace the static Name column template with a conditional template:
       - read-only when `!EnableMemberNameEdit || !_canManage || not editing this row`
       - editable when in edit mode for the row (textbox + Save / Reset / Cancel)
       - `(override)` muted tag when `Member.Name` differs from the corresponding `User.Name`
12. [x] Wire Save / Reset / Cancel to `TeamManagementService.SetMemberNameAsync` + `OnMemberNameChanged` callback + reload

### Tests
13. [x] `TeamServiceBase`: `SetMemberNameAsync` calls protected method, evicts cache, fires `TeamsListChangedEvent` (use existing `TestTeamService`)
14. [x] `AuditingTeamServiceDecorator`: `set-member-name` action logs success / failure
15. [x] Smoke test confirming `TeamComponent` exposes `EnableMemberNameEdit` + `OnMemberNameChanged` parameters

### Verify & ship
16. [x] Full build + test suite passes — 255 tests pass (7 new)
17. [ ] Archive plan, delete plan/, final commit, push, PR

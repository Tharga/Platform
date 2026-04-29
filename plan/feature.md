# Feature: team-member-name-edit

**Originating branch:** master
**Date started:** 2026-04-29

## Goal

Add an opt-in inline edit affordance for `Member.Name` in `<TeamComponent>`'s member grid. Today there's no UI to edit the per-team display-name override or to clear it back to the global `User.Name`. PlutusWave (feature 20) needs this to drop a workaround `MemberNamesPanel`.

The feature must be off by default (so existing consumers see no UI change) and toggleable via a parameter on the component.

## Scope

### Domain layer (Tharga.Team)

- **New method on `ITeamService`:** `Task SetMemberNameAsync(string teamKey, string userKey, string name)` — `name = null` clears the override.
- **New method on `ITeamManagementService`** with `[RequireScope(TeamScopes.Manage)]` (reusing the existing scope rather than introducing a new one — the request explicitly says either is fine, and `team:manage` is already what gates rename, so display-name edits fit naturally).
- **`TeamServiceBase`:**
  - New `protected abstract Task SetTeamMemberNameAsync(string teamKey, string userKey, string name)`
  - Public `SetMemberNameAsync` calls it, evicts the member cache, raises `TeamsListChangedEvent`. Audit pickup is automatic via existing `AuditingTeamServiceDecorator` (just need to extend it).

### Storage layer (Tharga.Team.MongoDB)

- **`ITeamRepository<TTeamEntity, TMember>`** — new `Task SetMemberNameAsync(string teamKey, string userKey, string name)`
- **`TeamRepository<...>`** — implement using `UpdateOneAsync` of the matching member entry (parallels existing `SetMemberRoleAsync` etc.)
- **`TeamServiceRepositoryBase`** — `SetTeamMemberNameAsync` override that delegates to `_teamRepository.SetMemberNameAsync`

### Audit (Tharga.Team.Service)

- Extend `AuditingTeamServiceDecorator` with the new method, action `"set-member-name"`. No Metadata changes needed.

### UI (Tharga.Team.Blazor `TeamComponent`)

- **New parameter:** `[Parameter] public bool EnableMemberNameEdit { get; set; }` (default `false`)
- **New parameter (optional):** `[Parameter] public EventCallback<MemberNameChangedArgs> OnMemberNameChanged { get; set; }`
- When `EnableMemberNameEdit == true` and the caller has the `team:manage` scope (`_canManage` is already tracked):
  - Each row in the Name column gets a small pencil button that swaps the cell to a textbox + Save / Reset / Cancel
  - **Save** writes the textbox value (whitespace → `null`) via `ITeamManagementService.SetMemberNameAsync`, fires `OnMemberNameChanged`, exits edit mode, reloads team
  - **Reset** writes `null` to clear the override (only visible when an override is currently set)
  - **Cancel** exits edit mode without writing
  - When `Member.Name` is set and a `User.Name` is also available, render a small `(override)` muted tag next to the name — discoverable polish, optional but easy to add
- When `EnableMemberNameEdit == false` (default), the existing read-only column renders unchanged

### Public type

```csharp
public sealed record MemberNameChangedArgs(string TeamKey, string MemberKey, string OldName, string NewName);
```

In `Tharga.Team` so consumers can reference it without taking a Blazor dependency.

## Persistence shape

**Shape A** (Platform persists, consumer observes via callback) — preferred per the request and matches the existing pattern (Platform owns team CRUD; consumers observe). **Shape B** (consumer-rejectable) is left as a follow-up if needed.

## Out of scope

- A new finer-grained `member:edit` scope — reusing `team:manage` is simpler, no breaking change for consumers. Can be added later if a consumer wants name edit decoupled from rename.
- Bulk inline edit, history of name changes, optimistic-concurrency conflict handling beyond the existing `TeamsListChangedEvent` re-render.

## Acceptance criteria

- [ ] `ITeamService.SetMemberNameAsync` and `ITeamManagementService.SetMemberNameAsync` exist; the latter is gated by `[RequireScope(TeamScopes.Manage)]`
- [ ] `TeamServiceBase.SetMemberNameAsync` evicts the member cache and raises `TeamsListChangedEvent`
- [ ] `TeamRepository<...>.SetMemberNameAsync` persists; `null` clears the override
- [ ] `AuditingTeamServiceDecorator` covers the new method (action: `set-member-name`)
- [ ] `TeamComponent` has a `EnableMemberNameEdit` parameter (default `false`); when off, no UI change vs. today
- [ ] When on + `_canManage`, pencil/Save/Reset/Cancel affordances render and work; whitespace input clears the override
- [ ] Optional `OnMemberNameChanged` callback fires after a successful save
- [ ] All existing tests still pass; new tests cover the service path (with cache eviction + event firing) and the audit decorator coverage

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete.

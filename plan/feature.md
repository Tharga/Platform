# Feature: Unauthenticated TeamServiceBase guards

Filed in `Requests.md` 2026-05-10 ("NullReferenceException in `TeamServiceBase` when current user is unauthenticated").

## Problem

`TeamServiceBase.GetTeamsAsync<TMember>()` does `var user = await GetCurrentUserAsync();` and immediately passes the (possibly null) result to `GetTeamsAsync(IUser user)`. The MongoDB override dereferences `user.Key`, throwing `NullReferenceException`. The same null-user pattern exists at **7 call sites** in `Tharga.Team/TeamServiceBase.cs`. Loading any page hosting `<TeamComponent />` before authentication (or after session expiry) triggers an unhandled exception, breaks the circuit, and bricks the page.

## Goal

Make `TeamServiceBase` resilient to a null current user. Each call site handles the unauthenticated case according to its semantic — read paths return empty; side-effect paths throw a meaningful exception.

## Scope

In-scope:

1. **Two centralized helpers on `TeamServiceBase`:**
    - `private async Task<IUser> GetCurrentUserAsync()` — kept as today (returns null silently). Used by read paths that handle null themselves.
    - `private async Task<IUser> RequireCurrentUserAsync()` (new) — throws `UnauthorizedAccessException("Authentication required.")` when the current user is null. Used by side-effect operations.
2. **Per-call-site refactor** — each of the 7 call sites uses the appropriate helper:
    | Line | Method | Treatment |
    |---|---|---|
    | 38 | `GetTeamsAsync()` | `if (user == null) yield break;` |
    | 48 | `GetTeamsAsync<TMember>()` | same |
    | 64 | `CreateTeamAsync(string name)` | `RequireCurrentUserAsync()` |
    | 127 | `RemoveMemberAsync` | `RequireCurrentUserAsync()` |
    | 213 | `SetMemberLastSeenAsync` | `if (user == null) return;` (touch op; benign no-op) |
    | 220 | `TransferOwnershipAsync<TMember>` | `RequireCurrentUserAsync()` |
    | 265 | `AssureAccessLevel<TMember>` (private gate) | `RequireCurrentUserAsync()` |
3. **Defensive null guard at the MongoDB boundary** — `TeamServiceRepositoryBase.GetTeamsAsync(IUser user)` returns `AsyncEnumerable.Empty<ITeam>()` for null input. Belt-and-suspenders: even if a future caller forgets the upstream guard, the data layer doesn't NRE.
4. **Tests** in `Tharga.Team.Service.Tests` — one per public API to cover the unauthenticated path.

Out of scope:

- UI changes in `TeamComponent.razor`. The component already renders no rows when `_teams` is empty, so the immediate-load path is correct after the server fix. Friendlier messaging for unauthenticated users (e.g. hiding the "Create new Team" button when no current user) is a separate UX concern.
- `TeamComponent`'s own `_user = await UserService.GetCurrentUserAsync()` may still be null after init. This was already the case; `_user.Key` accesses inside `@foreach (var team in _teams)` are unreachable when `_teams` is empty.
- Auditing decorators that wrap `TeamServiceBase` — they operate above this layer and are unaffected.
- README/docs change.

## Approach

### Server change

In `Tharga.Team/TeamServiceBase.cs`:

1. Add private `RequireCurrentUserAsync()`:
    ```csharp
    private async Task<IUser> RequireCurrentUserAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) throw new UnauthorizedAccessException("Authentication required.");
        return user;
    }
    ```

2. Refactor each public method per the table above.

### MongoDB boundary guard

In `Tharga.Team.MongoDB/TeamServiceRepositoryBase.cs`:

```csharp
protected override IAsyncEnumerable<ITeam> GetTeamsAsync(IUser user)
{
    if (user == null) return AsyncEnumerable.Empty<ITeam>();
    return _teamRepository.GetTeamsByUserAsync(user.Key);
}
```

### Tests

In `Tharga.Team.Service.Tests` — new file `UnauthenticatedTeamServiceTests.cs`:

- `GetTeamsAsync_Unauthenticated_ReturnsEmpty`
- `GetTeamsAsync_Generic_Unauthenticated_ReturnsEmpty`
- `CreateTeamAsync_Unauthenticated_Throws`
- `RemoveMemberAsync_Unauthenticated_Throws`
- `SetMemberLastSeenAsync_Unauthenticated_DoesNotThrow`
- `TransferOwnershipAsync_Unauthenticated_Throws`

All use `TestTeamService` with the mocked `IUserService.GetCurrentUserAsync()` returning null.

## Acceptance criteria

- All 7 `GetCurrentUserAsync()` call sites in `TeamServiceBase` handle null appropriately (empty yield, no-op, or `UnauthorizedAccessException`).
- `TeamServiceRepositoryBase.GetTeamsAsync(IUser user)` returns empty for null input.
- 6 new tests cover the unauthenticated path; existing 274 tests still pass.
- `dotnet build -c Release` clean.
- A consumer's sample app loaded by an unauthenticated principal no longer crashes the circuit on `<TeamComponent />` initial render.
- Bundled with the close-out commit from the previous feature (`fa36d13`) per user preference.

## Done condition

User confirms manually on the sample app:
- Loading the team page without authentication shows an empty team list (or a friendly empty state) instead of crashing the circuit.
- Authenticated flow is unchanged.

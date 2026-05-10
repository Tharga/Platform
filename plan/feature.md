# Feature: Stable Member.Key + name promotion + user self-edit

Filed in `Requests.md` 2026-05-10 ("TeamComponent inline name-edit broken for invited members"). Scope expanded during planning to include (a) name promotion on invitation accept, and (b) user self-edit of their own member row that propagates to `User.Name`.

## Problem

Invited (not-yet-accepted) team members are commonly created with `Member.Key = null` (the canonical pattern in the sample's `CreateTeamMember`). Two downstream effects:

1. **UI** — the inline name-edit gate uses `_editingMemberKey == context.Key`. When `_editingMemberKey` is reset to `null` after Cancel/Save, `null == null` matches indefinitely → edit mode sticks; opening one invited row's editor puts every invited row into edit mode at once.
2. **Server** — `TeamRepository.SetMemberNameAsync` does `team.Members.Single(x => x.Key == userKey)` and `.Where(x => x.Key != userKey)`. Multiple invited members (all `Key=null`) cause `.Single` to throw and `.Where` to strip every null-keyed sibling on save.

Separately, the admin-entered invitation name (`Member.Name`) is the only place a new user's display name is captured before the user signs in. Today, on accept, that name stays as a per-team override on `Member.Name` and is never promoted to `User.Name`. If the IdP doesn't supply a name, the user shows up as "Unknown" everywhere except the inviting team.

A third gap: today only callers with `team:manage` can edit the inline name. A user has no way to update their own display name from the team UI — they'd need a separate "edit profile" page that doesn't exist. Editing your own name should be a self-service action that updates the global `User.Name` (the truth) and clears the per-team override in the team where the edit happened.

## Goal

1. Eliminate the null-`Member.Key` anomaly: invited members get a stable, generated key from creation.
2. On accept, promote the admin-entered `Member.Name` into `User.Name` if the user has no name yet, then clear the per-team override. The IdP claim wins when present; admin's invitation name is the fallback; per-team override goes away once the user has a real identity.
3. Allow users to edit their own member row. Self-edit always overwrites `User.Name` and clears `Member.Name` in the current team. Other teams retain whatever override their admins set.

## Scope

In-scope:

1. **Auto-generate `Member.Key` on invitation** — `TeamServiceRepositoryBase.AddTeamMemberAsync`: if `memberModel.Key` is null/empty, assign `Guid.NewGuid().ToString()`. Symmetric to existing `Invitation` and `State` defaults in the same method.
2. **Promote Member.Name → User.Name on accept (only-if-empty rule)** — at acceptance, if `User.Name` is currently null/empty, seed it with the captured `Member.Name`. Always clear `Member.Name` on accept regardless.
3. **User self-edit own row** — show the inline name-edit pencil on the current user's own member row regardless of `team:manage` (so any user can edit their own name). On save: always overwrite `User.Name`, always clear `Member.Name` in the current team. Admin-edit on other rows is unchanged (writes per-team override).
4. **UI defensive gate (legacy data)** — `TeamComponent.razor`: change `_editingMemberKey == context.Key` → `!string.IsNullOrEmpty(_editingMemberKey) && _editingMemberKey == context.Key`. Belt-and-suspenders for any team document that still has null-keyed members from before this fix.

Confirmed during planning, no change needed:

- `TeamRepository.SetInvitationResponseAsync` already swaps `Key = userKey` on accept (and on reject, with state=Rejected).

Out of scope:

- Reset-button polish for invited rows. (User declined.)
- Migration of existing data with `Key=null`. (UI gate prevents the symptom; existing nulls heal naturally as invitations flow through accept/reject.)
- Server-side `.Single` resilience inside `TeamRepository.SetMemberNameAsync`. (Once `Member.Key` is unique by construction the existing `.Single` is correct.)
- Per-team alias for self (i.e., a user picking different names per team). The chosen design treats self-edit as global identity.
- Separate "Edit profile" page outside `TeamComponent`. The team grid is the only edit surface delivered here.

## Approach

### A. Auto-generate Member.Key (low-risk)

In `Tharga.Team.MongoDB/TeamServiceRepositoryBase.cs`, inside `AddTeamMemberAsync`, after `CreateTeamMember` and adjacent to the existing `Invitation` / `State` auto-generation:

```csharp
if (string.IsNullOrEmpty(memberModel.Key))
{
    memberModel = memberModel with { Key = Guid.NewGuid().ToString() };
}
```

### B. Name promotion on accept (cross-repo bridge)

The plumbing must reach across the team-repo and user-repo boundaries. Chosen path: route via `IUserService` so `TeamServiceRepositoryBase`'s constructor signature does not change.

1. **`Tharga.Team.MongoDB/IUserRepository.cs`** — add two methods:
    - `Task<TUserEntity> GetByKeyAsync(string userKey)` — lookup-by-Key (today only `GetAsync(identity)` exists).
    - `Task UpdateAsync(TUserEntity user)` — replace the whole user document.

2. **`Tharga.Team.MongoDB/UserRepository.cs`** — implement both via `_collection.GetOneAsync(x => x.Key == userKey)` and `_collection.ReplaceOneAsync` respectively.

3. **`Tharga.Team/IUserService.cs`** — add two methods (different semantics):
    - `Task SeedUserNameAsync(string userKey, string name)` — only-if-empty. Used by `TeamServiceBase` on invitation-accept.
    - `Task SetUserNameAsync(string userKey, string name)` — always overwrites. Used by user self-edit.

4. **`Tharga.Team/UserServiceBase.cs`** — provide virtual `Task.CompletedTask` no-op defaults for both so consumers extending the base without overriding aren't broken.

5. **`Tharga.Team.MongoDB/UserServiceRepositoryBase.cs`** — override both:
    - Shared helper: `var user = await _userRepository.GetByKeyAsync(userKey);` if null → log warning + return.
    - `SeedUserNameAsync`: if `!string.IsNullOrEmpty(user.Name)` → return (only-if-empty). Else write + cache invalidate.
    - `SetUserNameAsync`: always write (`user with { Name = name }` → `UpdateAsync`) + cache invalidate.

6. **`Tharga.Team/TeamServiceBase.cs`** — orchestrate in `SetInvitationResponseAsync`:
    - On `accept == true`: pre-fetch the team, find the member with `Invitation.InviteKey == inviteKey`, capture `member.Name` as `seedName`.
    - Call existing `SetTeamMemberInvitationResponseAsync(teamKey, userKey, inviteKey, true)` — already swaps `Key = userKey` and clears `Invitation`. **Update** the underlying `TeamRepository.SetInvitationResponseAsync` to also set `Name = null` on accept (1-line addition to the existing `with` block).
    - If `seedName` is non-empty, `await _userService.SeedUserNameAsync(userKey, seedName)`.

### C. User self-edit own row (UI + orchestration)

In `Tharga.Team.Blazor/Features/Team/TeamComponent.razor`:

1. **Pencil visibility** — change the gate from `EnableMemberNameEdit && _canManage` to `EnableMemberNameEdit && (_canManage || context.Key == _user.Key)` so a non-admin user sees the pencil on their own row only.
2. **Save behaviour** — `SaveMemberName` branches:
    - If `member.Key == _user.Key` (self-edit): `await UserService.SetUserNameAsync(_user.Key, newName); await TeamManagementService.SetMemberNameAsync(team.Key, _user.Key, null);` Then reload + fire `OnMemberNameChanged` with the (cleared) override and the new identity.
    - Else (admin-edit on another row): unchanged — `await TeamManagementService.SetMemberNameAsync(...)` writes the per-team override.
3. **Reset button** — hide for self-edit (`context.Key == _user.Key && !_canManage`); for self there is no override-vs-default distinction.

### D. UI defensive gate

In `Tharga.Team.Blazor/Features/Team/TeamComponent.razor`, line ~65:

```razor
@if (EnableMemberNameEdit && (_canManage || context.Key == _user.Key) && !string.IsNullOrEmpty(_editingMemberKey) && _editingMemberKey == context.Key)
```

(Note: the visibility changes from C.1 and the null guard are merged into a single condition.)

## Acceptance criteria

- New invited members in MongoDB carry a non-null `Member.Key` (manual verify on sample app).
- Inline name-edit on an invited member returns the row to read-only display after save (manual verify).
- Multiple invited members do not all enter edit mode together when one is opened (manual verify).
- On invitation accept: `User.Name` is seeded from `Member.Name` if previously empty; never overwritten if present. `Member.Name` is cleared on accept regardless. (Manual verify with a fresh user; unit tested for the orchestration logic.)
- A non-admin user can edit their own row in `<TeamComponent>` and the change updates `User.Name` globally + clears `Member.Name` in the current team. Other teams' admin-set overrides for that user are unaffected. (Manual verify across two teams; unit tested.)
- `dotnet build -c Release` clean.
- `dotnet test -c Release` — all existing tests still pass; new orchestration tests added in `Tharga.Team.Service.Tests` for: invitation-accept seed flow (called when prior name was set, NOT called when empty, NOT called on reject), user self-edit branch (calls `SetUserNameAsync` + clears Member.Name) vs admin-edit branch (calls only `SetMemberNameAsync`).
- Bundled with the close-out commit from #61.

## Done condition

User confirms manually on the sample app:
- A new invitation creates a member with non-null `Key` and non-cleared `Name` while invited.
- After accept (with a fresh user whose IdP returns no name), `User.Name` carries the admin-entered invitation name and `Member.Name` is null.
- After accept (with a user whose IdP already returns a name), `User.Name` is unchanged and `Member.Name` is null.

# Plan: Stable Member.Key + name promotion + user self-edit

Branch: `feature/invited-member-edit-fix` (already carries the close-out commit `69c8226 feat: teammember-duplicate-fix complete` — bundled per user preference to avoid an extra PR).

## Steps

- [x] **1. Confirm plan with user.** Done.
- [x] **2. Auto-generate `Member.Key` in `AddTeamMemberAsync`.** `TeamServiceRepositoryBase.cs` — symmetric block alongside existing Invitation/State defaults.
- [x] **3. Extend `IUserRepository`.** Added `GetByKeyAsync(string userKey)` and `SetNameAsync(string userKey, string name)`. Note: shipped a focused `SetNameAsync` (partial MongoDB update via `UpdateDefinitionBuilder.Set`) instead of a generic `UpdateAsync(TUserEntity)` because `IUser.Name` is read-only on the interface and `with { Name = … }` doesn't compile against the constraint `where TUserEntity : EntityBase, IUser`. The focused-method form is also more efficient (no full-document round-trip).
- [x] **4. Extend `IUserService`.** Added `SeedUserNameAsync` (only-if-empty) and `SetUserNameAsync` (always-overwrites) with `Task.CompletedTask` virtual defaults on `UserServiceBase`, real implementations in `UserServiceRepositoryBase<TUserEntity>` (GetByKey → check rule → SetNameAsync → InvalidateUserCache). Added `protected void InvalidateUserCache(string identity)` on `UserServiceBase` for the cache invalidation.
- [x] **5. Clear `Member.Name` on accept.** Added `Name = null,` to the `member with { ... }` accept block in `TeamRepository.SetInvitationResponseAsync`.
- [x] **6. Orchestrate seeding in `TeamServiceBase.SetInvitationResponseAsync`.** Pre-capture via new virtual hook `GetInvitedMemberNameAsync(teamKey, inviteKey)` (default returns null on `TeamServiceBase`; overridden in `TeamServiceRepositoryBase` using its typed access to `team.Members`). After response call, if seedName non-empty, `await _userService.SeedUserNameAsync(userKey, seedName)`.
- [x] **7. Self-edit branch in `TeamComponent.razor`.** `SaveMemberName` branches on `member.Key == _user.Key`. Self: `UserService.SetUserNameAsync` + `SetMemberNameAsync(..., null)`. Other: existing per-team override path. Empty self-edit input is treated as a no-op cancel.
- [x] **8. UI gates in `TeamComponent.razor`.** New `canEditThisRow` / `isSelfEdit` locals at the top of the Name template. Edit-mode visibility now `canEditThisRow && !string.IsNullOrEmpty(_editingMemberKey) && _editingMemberKey == context.Key`. Pencil visible whenever `canEditThisRow`. Reset button hidden when `isSelfEdit`.
- [x] **9. Tests.** Added in `Tharga.Team.Service.Tests`:
    - `SetInvitationResponseSeedTests` — 4 tests: accept with name calls Seed once, accept with empty/whitespace name doesn't call Seed (×2), reject doesn't call Seed.
    - `UserServiceBaseDefaultsTests` — 2 tests: both new virtual defaults are no-ops.

    UI self-edit branch not unit-tested (no Razor harness). Manual verification will cover it.
    No tests for `IUserRepository.SetNameAsync` / `GetByKeyAsync` — no MongoDB integration test project in repo.
- [x] **10. Build and run all tests.** Build clean (0 warnings, 0 errors). 274 / 274 tests passing (was 268, +6 new). 145 Service + 37 Mcp + 92 Blazor.
- [~] **11. Manual verification on sample.** Run the sample app:
    - Invite a new user with a name. Inspect MongoDB → confirm `Members[].Key` is a non-null GUID and `Members[].Name` is the admin-entered value.
    - Edit the invited member's name in the UI, save. Confirm row returns to read-only display showing the new name.
    - Invite a second user; open one's edit pencil — confirm the other invited row stays read-only.
    - Accept the invitation as the invitee (using a fresh user whose IdP-returned name is empty or present). Confirm:
      - `Member.Key` was swapped to `User.Key`.
      - `Member.Name` is now null.
      - `User.Name` was seeded from `Member.Name` only if it was empty before.
    - **Self-edit:** as a non-admin member of a team, click the pencil on your own row, change the name, save. Confirm:
      - `User.Name` updated globally.
      - `Member.Name` is null in this team.
      - In a *different* team where the same user has an admin-set override, that override is unchanged.
- [ ] **12. Commit.** Conventional commit `fix: stable Member.Key for invited members + Member.Name promotion (accept + self-edit)`. Includes `plan/`.
- [ ] **13. Hand back to user for testing.**
- [ ] **14. (After user OK) Open PR.** Push and open PR against `master`. Description doubles as release notes (per `mission.md`); cover the close-out + all three areas (null-Key fix, accept-time promotion, self-edit).
- [ ] **15. (After PR merged) Close out.** Archive `plan/feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/invited-member-edit-fix.md`, delete `plan/`, final commit `feat: invited-member-edit-fix complete`.

## Notes

- No NuGet upgrades needed (`dotnet outdated` reports nothing).
- The branch already carries the close-out commit for the previous feature (`69c8226`). The single PR will include: `plan/` deletion (close-out of #61) + new `plan/` + this fix's code changes. Net result on master: clean.
- **API surface change** (consumer-visible): `IUserRepository` gains `GetByKeyAsync` + `UpdateAsync`. `IUserService` gains `SeedUserNameAsync` + `SetUserNameAsync`. All four are non-breaking for consumers extending `UserServiceRepositoryBase` / using `IUserRepository<TUserEntity>` as DI-only (typical pattern). External implementers of those interfaces directly would see breakage — none known in this codebase. Suggest a minor version bump on next release.
- **Decided against** adding `TUserEntity` as a third type-parameter to `TeamServiceRepositoryBase`. Would have been a breaking change for every consumer extending it (`Tharga.Platform.Sample/Framework/Team/TeamService.cs`, plus external). Routing through `IUserService` keeps `TeamServiceRepositoryBase`'s ctor stable.
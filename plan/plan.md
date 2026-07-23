# Plan: entra-user-management

## Steps

- [x] **0. NuGet update check** — `dotnet outdated` run 2026-07-23 across the whole solution: no outdated dependencies. Nothing to apply.

- [x] **1. Core abstractions (`Tharga.Team`)** — done 2026-07-23. Added `IUser.DirectoryId`/`LastSeen` (default members), `IUserDirectoryService` (+ `DirectoryUser`, `DirectoryVerificationResult`, `DirectoryUserStatus`), `SystemUserScopes.Manage`, `IUserManagementService` (+ `UserVerificationResult`, `UserDeleteResult`). Decisions: directory delete takes directoryId (works for directory-only users); `DeleteUserAsync` returns `UserDeleteResult` instead of throwing on directory failure (local delete never rolled back); stamping seam deferred to step 3 as planned. Build + 669 tests green.
  - `IUser`: add default interface members `DateTime? LastSeen => null;` and `string DirectoryId => null;` (non-breaking, same pattern as `Name`).
  - New `IUserDirectoryService`: `Task<DirectoryVerificationResult> VerifyUserAsync(IUser user, CancellationToken)`, `Task DeleteUserAsync(string directoryId, CancellationToken)` (directory id, not `IUser` — works for linked and directory-only users alike), `IAsyncEnumerable<DirectoryUser> GetUsersAsync(CancellationToken)` (streamed/paged enumeration); `DirectoryUser` record (DirectoryId, Name, EMail/UPN, Enabled); `DirectoryVerificationResult` record + `DirectoryUserStatus` enum (Found / NotFound / Disabled / NotLinked) with the resolved directory id (for relink).
  - New `SystemUserScopes` class: `Manage = "users:manage"` (mirror `SystemTeamScopes`).
  - New `IUserManagementService`: `VerifyUserAsync(string userKey)`, `IAsyncEnumerable<...> VerifyAllAsync()`, `DeleteUserAsync(string userKey, bool deleteFromDirectory)`, `IAsyncEnumerable<DirectoryUser> GetDirectoryOnlyUsersAsync()` — all with `[RequireScope(SystemUserScopes.Manage)]`.
  - Extend `IUserService` (or repository seam) with what stamping/backfill needs — decide exact seam while implementing.

- [x] **2. Persistence (`Tharga.Team.MongoDB`)** — done 2026-07-23. `IUserRepository` + `UserRepository`: `SetLastSeenAsync`/`SetDirectoryIdAsync`/`DeleteAsync` — the two field writes are **opt-in by entity shape** (reflection guard: no-op unless the entity declares the property, since updating an undeclared interface default member fails at driver render time); delete default-throws (silent no-op would hide a missing impl). `ITeamRepository`/`TeamRepository`: `RemoveMemberFromAllTeamsAsync(userKey)` → `Task<int>` (strips member entries in any state, returns team count). Service seam: `IUserService` gained `SetUserLastSeenAsync`/`SetUserDirectoryIdAsync` (default no-op) + `DeleteUserAsync` (default throws), virtuals on `UserServiceBase`, overridden in `UserServiceRepositoryBase`; `ITeamService.RemoveUserFromAllTeamsAsync` (plain member per `GetAllTeamsAsync` precedent) with `TeamServiceBase` virtual-throw + Mongo override; both team decorators forward it — authorization requires `users:manage`, audit logs `remove-member-all` with new `AuditMetadataKeys.MemberTeamCount`. Added `InternalsVisibleTo` for MongoDB.Tests (sibling precedent). 13 new tests incl. an update-render test proving the interface-member expressions translate against the entity class map. Note for step 4: oid capture at `CreateUserEntityAsync` is consumer code — the framework backfill (step 3) covers it; document in step 8/10.
  - Deferred decision: audit of per-team removals during user delete is a single `remove-member-all` entry (count metadata), not N `remove-member` entries — team timelines won't show individual rows; revisit if requested.

- [x] **3. LastSeen stamping + oid backfill (`Tharga.Team`)** — done 2026-07-23. `UserServiceBase.GetCurrentUserAsync` now touches the user on every resolve: throttled LastSeen stamp (virtual `LastSeenStampInterval`, default 15 min; `TimeSpan.Zero` = every resolve, null = disabled; per-process static throttle map) + one-shot-per-process `DirectoryId` backfill from the oid claim (one attempt guard prevents cache thrash when the entity doesn't persist the field). New `DirectoryClaimTypes` (raw `oid` + mapped objectidentifier, `GetDirectoryId()` extension). Whole touch wrapped in try/catch + optional `ILogger` (TeamServiceBase ctor precedent) so storage failures never break the auth pipeline. 8 new tests (throttle, zero/null interval, failure tolerance, backfill both claim forms, one-shot). Full suite 690 green. Note: interval config via options builder deferred to step 6 (consumers can override the virtual today).

- [x] **4. User management service (`Tharga.Team.Service`)** — done 2026-07-23. `UserManagementService` (verify + relink-on-email-fallback, `VerifyAllAsync` stream, delete with directory id resolved BEFORE local delete, `GetDirectoryOnlyUsersAsync` diff by directory id + case-insensitive email). `AuthorizationUserManagementServiceDecorator` (users:manage on every op, incl. inside the async iterators) + `AuditingUserManagementServiceDecorator` (feature "user": `verify` with status, `verify-all` single summary entry with count via try/finally around the stream, `delete` with team count + directory outcome; directory-only listing not audited, consistent with team enumeration). Added `IUserService.GetUserByKeyAsync` seam (default scans `GetAsync()`, Mongo overrides with direct read) and `UserDeleteResult.RemovedTeamCount`. New audit metadata keys: `user.key`, `directory.status`, `directory.deleted`, `directory.error`, `verify.count`. 29 new tests; suite 719 green. DI wiring deferred to step 6 as planned.

- [x] **5. Entra provider (`Tharga.Team.Entra` + tests)** — done 2026-07-23. Went with the light stack: `Azure.Identity` 1.21.0 (only new package) + `FrameworkReference Microsoft.AspNetCore.App` (Service-project precedent) for HttpClientFactory/options/config-binding — no Microsoft.Graph SDK. `EntraUserDirectoryService` (verify by oid — 404 = NotFound with deliberately NO email fallback since a broken link is a finding; email/UPN OData lookup with quote escaping for unlinked users, resolved oid returned for relink; DELETE = Graph soft delete, 404 throws informative; paged streaming enumeration via `@odata.nextLink`, `$top=999`). `IEntraTokenProvider` is a public seam (default `CredentialEntraTokenProvider`: options `Credential` override or `ClientSecretCredential`; MSAL caches in-memory). `AddThargaEntraUserDirectory(config, configure)` binds from the existing `AzureAd` section (TenantId/ClientId/ClientSecret). Package README written (incl. Graph permission table). Both projects added to sln AND to both CI pack steps in `.github/workflows/build.yml` (would otherwise never publish). 17 new tests (scripted HttpMessageHandler); suite 736 green.

- [~] **6. Registration & options wiring**
  - `ThargaPlatformOptions.AddUserDirectoryService<T>()` (+ LastSeen interval option); conditional registration in `ThargaPlatformRegistration` following the email-sender pattern; decorator wiring following `DecorateWithAudit`/`DecorateWithAuthorization`.
  - Convenience `AddThargaEntraUserDirectory(...)` extension in `Tharga.Team.Entra`.
  - Register `users:manage` scope + role mapping via the existing system-scope registry path (opt-in like `teams:read`/#128).

- [ ] **7. Blazor UI (`Tharga.Team.Blazor`, `Features/User/`)**
  - `UsersListView`: "Last seen" column; verify-status badge column (empty until verified); per-row Verify action; "Verify all" toolbar button (streams `VerifyAllAsync`, updates badges); Delete action opening a confirm dialog with the opt-in "Also delete from Entra" checkbox (only rendered when a directory service is registered; whole action set gated by `users:manage`).
  - New "Entra only" tab on `UsersView` (alongside Users / Teams): read-only list of directory-only users (name, email/UPN, enabled badge), loaded on demand via a Load/Refresh button streaming `GetDirectoryOnlyUsersAsync` (progressive render, tenants can be large). Rendered only when a directory service is registered; gated by `users:manage`.
  - Keep `ActionsTemplate` untouched for consumer extensions.
  - Tests in `Tharga.Team.Blazor.Tests` (follow existing component-test patterns).

- [ ] **8. Sample app (`Tharga.Platform.Sample`)**
  - Wire `AddThargaEntraUserDirectory` with config placeholders in `appsettings.json`; map `users:manage` to the Developer role; reconcile `UsersPage`/`AppUserAdminService` with the new framework delete (sample should use `IUserManagementService` now).
  - Manual smoke test in the sample.

- [ ] **9. Full build + test pass** — `dotnet build -c Release && dotnet test -c Release`; push branch for user testing; ask for feedback. Do NOT open the PR yet.

- [ ] **10. Docs (`docs:` commit, before close-out)** — update `README.md` AND `docs/articles/` (likely a new `user-management.md` article + toc entry + touch `implementation-guide.md`); document Entra app-registration permissions (`User.Read.All` verify + Entra-only listing, `User.ReadWrite.All` delete), the opt-in delete semantics, the soft-delete window, and the "Entra only" view.

- [ ] **11. Close-out (after user confirms done)** — re-run `dotnet outdated` (apply any new updates), archive `feature.md` to the external plan directory `done/`, `git rm -r plan`, final commit `feat: entra-user-management complete`, push, open PR → `master` (PR description = release notes; suggest minor version bump — additive API).

## Notes / decisions

- 2026-07-23: Feature started. Design decisions agreed: per-user + bulk verify (no background sweep); single delete action with opt-in Entra checkbox; throttled per-request LastSeen; new optional package `Tharga.Team.Entra` for the Graph dependency.
- 2026-07-23: Scope addition (user request): "Entra only" view listing directory users with no local record. Read-only for now; directory delete reshaped to take a directory id so a later delete/invite action on those rows stays cheap. Enumeration uses the same `User.Read.All` permission verify already needs — no new Graph permissions.
- Packages were fully up to date at feature start — step 0 is a no-op.

## Last session

Feature branch `feature/entra-user-management` created from `master`; plan written; awaiting user confirmation of the plan before any code changes (step 1 marked in progress but not started).

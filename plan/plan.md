# Plan: entra-user-management

## Steps

- [x] **0. NuGet update check** — `dotnet outdated` run 2026-07-23 across the whole solution: no outdated dependencies. Nothing to apply.

- [x] **1. Core abstractions (`Tharga.Team`)** — done 2026-07-23. Added `IUser.DirectoryId`/`LastSeen` (default members), `IUserDirectoryService` (+ `DirectoryUser`, `DirectoryVerificationResult`, `DirectoryUserStatus`), `SystemUserScopes.Manage`, `IUserManagementService` (+ `UserVerificationResult`, `UserDeleteResult`). Decisions: directory delete takes directoryId (works for directory-only users); `DeleteUserAsync` returns `UserDeleteResult` instead of throwing on directory failure (local delete never rolled back); stamping seam deferred to step 3 as planned. Build + 669 tests green.
  - `IUser`: add default interface members `DateTime? LastSeen => null;` and `string DirectoryId => null;` (non-breaking, same pattern as `Name`).
  - New `IUserDirectoryService`: `Task<DirectoryVerificationResult> VerifyUserAsync(IUser user, CancellationToken)`, `Task DeleteUserAsync(string directoryId, CancellationToken)` (directory id, not `IUser` — works for linked and directory-only users alike), `IAsyncEnumerable<DirectoryUser> GetUsersAsync(CancellationToken)` (streamed/paged enumeration); `DirectoryUser` record (DirectoryId, Name, EMail/UPN, Enabled); `DirectoryVerificationResult` record + `DirectoryUserStatus` enum (Found / NotFound / Disabled / NotLinked) with the resolved directory id (for relink).
  - New `SystemUserScopes` class: `Manage = "users:manage"` (mirror `SystemTeamScopes`).
  - New `IUserManagementService`: `VerifyUserAsync(string userKey)`, `IAsyncEnumerable<...> VerifyAllAsync()`, `DeleteUserAsync(string userKey, bool deleteFromDirectory)`, `IAsyncEnumerable<DirectoryUser> GetDirectoryOnlyUsersAsync()` — all with `[RequireScope(SystemUserScopes.Manage)]`.
  - Extend `IUserService` (or repository seam) with what stamping/backfill needs — decide exact seam while implementing.

- [~] **2. Persistence (`Tharga.Team.MongoDB`)**
  - `IUserRepository`/`UserRepository`: `SetLastSeenAsync(userKey, timestamp)`, `SetDirectoryIdAsync(userKey, directoryId)`, `DeleteAsync(userKey)`.
  - `UserServiceRepositoryBase`: capture `oid` claim into `DirectoryId` at `CreateUserEntityAsync` time (document for consumers), expose delete.
  - Team side: `RemoveMemberFromAllTeamsAsync(userKey)` on the repository base + `TeamRepository` (reuse the existing member-removal write pattern).
  - Tests in `Tharga.Team.MongoDB.Tests`.

- [ ] **3. LastSeen stamping + oid backfill (`Tharga.Team` / service layer)**
  - Stamp in the `GetCurrentUserAsync` resolve path (`UserServiceBase`) — NOT in `TeamServerClaimsTransformation` (it early-returns when no team cookie is set). Throttle: at most one write per user per interval; interval configurable via options (default 15 min). Piggyback the existing static user cache for the throttle bookkeeping.
  - Backfill `DirectoryId` from the `oid` claim on resolve when the stored value is empty.
  - Tests: throttling behavior, backfill, no-op when claims lack `oid`.

- [ ] **4. User management service (`Tharga.Team.Service`)**
  - Implementation of `IUserManagementService`: verify via `IUserDirectoryService` (null-tolerant → NotConfigured/NotLinked handling), delete = remove from all teams → delete user record → invalidate user cache; `deleteFromDirectory: true` additionally calls the directory delete (by the user's `DirectoryId`, resolving via email fallback first if unset). Local delete completes even if the directory call fails — surface the directory error to the caller.
  - `GetDirectoryOnlyUsersAsync`: build the local match set (all `DirectoryId`s + emails, one pass over `IUserService.GetAsync()`), then stream `IUserDirectoryService.GetUsersAsync()` and yield entries matching neither directory id nor email (email fallback prevents pre-feature local users without an `oid` from being falsely reported as Entra-only).
  - `AuthorizationUserManagementDecorator` (requires `users:manage`) + `AuditingUserManagementDecorator` (`user-verify`, `user-delete`, `user-directory-delete` entries) following the existing team decorators.
  - Tests in `Tharga.Team.Service.Tests`.

- [ ] **5. Entra provider (new project `Tharga.Team.Entra` + tests)**
  - `EntraUserDirectoryService : IUserDirectoryService` — app-only Graph calls (decide during implementation: `Azure.Identity` + `HttpClient` against Graph REST vs the full `Microsoft.Graph` SDK; prefer the lighter dependency). Config: reuse `AzureAd` section values (TenantId/ClientId) + client secret/credential from its own section; validate at startup.
  - Verify: lookup by `DirectoryId` (oid) → fallback by mail/UPN using `EMail` → return resolved oid so the caller can relink; map `accountEnabled: false` → Disabled; 404 → NotFound; no oid and no email match → NotLinked.
  - Delete: `DELETE /users/{oid}` (soft delete, 30-day restore window — document this).
  - Enumerate: `GET /users` with `$select=id,displayName,mail,userPrincipalName,accountEnabled`, following `@odata.nextLink` paging; yield per page so large tenants stream instead of buffering.
  - New csproj mirrors packaging metadata of sibling packages; add to solution; unit tests with faked HTTP/token layer.

- [ ] **6. Registration & options wiring**
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

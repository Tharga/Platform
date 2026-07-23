# Feature: entra-user-management

## Goal

Give administrators visibility into user activity and a way to reconcile the local user store with Microsoft Entra ID: see when each user was last active, verify that a user still exists (and is enabled) in Entra, and delete a user from the service — optionally also from Entra — in one audited, authorized operation.

## Background (current state)

- Users live in the shared Mongo `User` collection (`IUser`: `Key`, `Identity`, `EMail`, `Name`), created lazily on first authenticated request. **No timestamps exist on the user record** — the only activity data is per-team-member `LastSeen`, stamped on team selection.
- `Identity` stores the OIDC `sub`/NameIdentifier claim. The Entra object id (`oid`) is **not** captured, and no Graph integration exists — only `Microsoft.Identity.Web` sign-in.
- There is no framework-level user deletion; only the sample app deletes user documents directly (`AppUserAdminService`). Team-member removal detaches a user from one team but never deletes the user record.

## Scope

1. **Per-user activity tracking** — new `LastSeen` (throttled per-request stamp via the user-service resolve path, default interval 15 min, configurable) and `DirectoryId` (Entra `oid`, captured from claims on user creation and backfilled for existing users on resolve) on the user record. Non-breaking: default interface members on `IUser`, like `Name`.
2. **Pluggable directory provider** — `IUserDirectoryService` abstraction in `Tharga.Team` (verify → Found / NotFound / Disabled / NotLinked; delete by directory id; enumerate directory users, paged/streamed), registered via the `ThargaPlatformOptions` builder following the email-sender/audit-logger pattern. Entra implementation in a **new package `Tharga.Team.Entra`** (keeps the Graph/credential dependency optional). Verification resolves by stored `DirectoryId` first, falls back to email/UPN lookup, and persists the found `oid` on success (relink).
3. **Framework user deletion + verification service** — `IUserManagementService` with `VerifyUserAsync`, bulk verify, and `DeleteUserAsync(userKey, deleteFromDirectory)`: removes the user from **all** teams, deletes the user record, and (opt-in) deletes the Entra user. Guarded by a new system scope `users:manage`, decorated with authorization + audit like the existing team service.
4. **Directory-only users ("Entra only") view** — `IUserManagementService.GetDirectoryOnlyUsersAsync()` diffs the enumerated directory users against the local store (matched by `DirectoryId`, falling back to email so pre-feature local users without an `oid` are not falsely reported) and returns those existing **only in Entra**. Loaded on demand (button/tab activation, streamed as Graph pages arrive — tenants can be large), never automatically.
5. **Admin UI** — extend `UsersView`/`UsersListView`: Last seen column, per-row Verify action + verify-status badge, a "Verify all" bulk action, and Delete with a confirmation dialog containing an explicit **opt-in** "Also delete from Entra" checkbox (off by default; only shown when a directory service is registered and the caller holds `users:manage`). New "Entra only" tab/section on `UsersView` listing directory-only users (name, email/UPN, enabled state), loaded on demand.
6. **Sample + docs** — wire the Entra provider in `Tharga.Platform.Sample`; update `README.md` and `docs/articles/` (both surfaces).

## Out of scope

- Scheduled/background re-verification sweeps (decided: per-user + bulk button only).
- Actions on directory-only users (invite to the platform, delete from Entra) — the "Entra only" view is read-only for now; the delete-by-directory-id abstraction makes a later action cheap to add.
- Restoring soft-deleted Entra users from the UI.
- Directory providers other than Entra (the abstraction allows them; none are built).
- Per-team-member `LastSeen` changes — the existing mechanism stays as is.

## Design decisions (agreed 2026-07-23)

- Verify: per-user action **plus** a bulk "Verify all" button. No background sweep.
- Delete: one action; local delete always, Entra delete via explicit opt-in checkbox (Graph delete is a 30-day-restorable soft delete but org-wide — hence opt-in, never default).
- LastSeen: per-request stamping, throttled to at most one write per interval per user.
- Feature starts now on `feature/entra-user-management` (from `master`, GitHub Actions strategy).

## Acceptance criteria

- [ ] User records gain `LastSeen` + `DirectoryId`; `LastSeen` is stamped on authenticated activity at most once per configured interval; `oid` is captured for new users and backfilled for existing users on their next resolve. Existing consumer entities compile unchanged (default interface members).
- [ ] `IUserDirectoryService` is registered via options; when not registered, all directory UI/actions degrade gracefully (hidden/disabled — no errors).
- [ ] Entra verify returns Found / NotFound / Disabled / NotLinked correctly (oid first, email fallback + relink), using app-only Graph credentials from configuration.
- [ ] `DeleteUserAsync` removes the user from every team, deletes the user record, writes an audit entry, and requires `users:manage`; with `deleteFromDirectory: true` it also deletes the Entra user and audits that separately. Failure to delete in Entra does not leave the local store half-deleted (local delete completes; directory failure is surfaced).
- [ ] Users list shows Last seen, verify status per row after Verify / Verify all, and the delete dialog with the opt-in Entra checkbox, all gated by `users:manage`.
- [ ] The "Entra only" view lists directory users with no matching local record (matched by `DirectoryId` with email fallback), loads on demand with streamed paging, and is gated by `users:manage`; hidden when no directory service is registered.
- [ ] Unit tests cover: stamp throttling, oid capture/backfill, verify resolution paths, delete (all-team removal + audit + authorization + directory opt-in), and UI gating. Full suite green.
- [ ] `README.md` and `docs/articles/` updated (both surfaces).

## Done condition

All acceptance criteria met, full test suite passes, user has tested from the pushed branch and confirmed, docs committed, `plan/` removed in the close-out commit, PR from `feature/entra-user-management` → `master` with release-note-level description.

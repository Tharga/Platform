# Plan: team-icons (Phase A)

## Steps

- [x] **0. NuGet update check** — `dotnet outdated` run 2026-07-24 across the whole solution: no outdated dependencies. Nothing to apply. (Mandatory feature-start step — no-op.)

- [x] **1. Core abstractions (`Tharga.Team`)** — done 2026-07-24. Two seams shipped: storage `IIconStore` (+ `IconContent`, `IconKind`, `IconOptions` [256 KB / png,jpeg,gif,webp,svg], `IconValidation`/`IconValidationResult` with content-type normalization); sourcing `IIconSource` → `IconImage`, `IconSubject` (team+user fields, user unused until B), `IIconResolver`/`IconResolver` (first-non-null in registration order), built-in `StoredIconSource` (reference → `IconRoute.Url`), `IconRoute` (shared `/_tharga/icon` base), `IconInitials` fallback. 24 tests in Service.Tests (`IconAbstractionsTests`, per the ResolveDisplayName precedent). Decision: resolver runs custom sources FIRST, `StoredIconSource` LAST — a custom source can override or defer (return null) to an explicitly-set icon. Full suite 792 green.
  - Original detail:
  - **Storage — `IIconStore`:** `Task<string> SaveAsync(IconKind kind, string ownerKey, byte[] data, string contentType, CancellationToken)`, `Task<IconContent> LoadAsync(string reference, CancellationToken)` (null when missing), `Task DeleteAsync(string reference, CancellationToken)`.
  - **Sourcing — `IIconSource`:** `Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken)` (null defers to the next source). `IconSubject` record (`IconKind Kind`, `string Key`, `string Name`, `string IconReference`; `string EMail`, `string DirectoryId` for Phase B — nullable/unused in A). `IconImage` record (`string Url`). `IIconResolver` composes registered sources in order, then an initials/default fallback (the resolver itself is built-in, not consumer-implemented).
  - Built-in `StoredIconSource : IIconSource` — when `subject.IconReference` is set, returns the serving-endpoint URL; else null.
  - Types: `IconKind` (`Team`, `User`); `IconContent` (`byte[] Data`, `string ContentType`); `IconOptions` (`MaxBytes` default 256 KB, `AllowedContentTypes` default png/jpeg/gif/webp/svg); `IconValidation` helper (size + content-type, clear error) shared by store and callers.
  - Tests: options defaults, validation accept/reject, resolver ordering (custom source overrides/defers, fallback to initials), StoredIconSource URL/null.

- [x] **2. Built-in Mongo store (`Tharga.Team.MongoDB`)** — done 2026-07-24. `IconEntity` (own `Key` reference, `Kind` as string, `OwnerKey`, `ContentType`, `byte[] Data`, `Size`, `CreatedUtc`), `IIconRepositoryCollection` + `IconRepositoryCollection` (collection `Icon` via new `ThargaTeamOptions.IconCollectionName`, unique Key index), `MongoIconStore : IIconStore` (validate → insert → return Key; load/delete by Key; blank ref short-circuits). Registered in `AddThargaTeamRepository`: `AddOptions<IconOptions>()` (so `IOptions<IconOptions>` resolves without the Blazor layer) + collection + `TrackMongoCollection` + `TryAddScoped<IIconStore, MongoIconStore>` (consumer override wins). 9 tests. Full suite 801 green.
  - Original detail:
  - `IconEntity : EntityBase` (`Kind`, `OwnerKey`, `ContentType`, `byte[] Data`, `Size`, `CreatedUtc`). `IIconRepositoryCollection` + `IconRepositoryCollection : DiskRepositoryCollectionBase<IconEntity>` (collection name from `ThargaTeamOptions.IconCollectionName`, default `"Icon"`).
  - `MongoIconStore : IIconStore` — save (validate → insert → return `Id` as reference), load (by id), delete (by id).
  - Register in `AddThargaTeamRepository`: add the collection + `TrackMongoCollection`, and `services.TryAddScoped<IIconStore, MongoIconStore>()` (consumer override wins). Add `IconCollectionName` to `ThargaTeamOptions`.
  - Tests: save returns a reference, load round-trips bytes+content-type, delete removes, oversize/disallowed-type rejected. (Follow existing MongoDB.Tests substitute-collection pattern.)

- [~] **3. Team-service icon operations (`Tharga.Team` + `Tharga.Team.MongoDB` + `Tharga.Team.Service`)**
  - `ITeamService.SetTeamIconAsync(teamKey, byte[] data, contentType)` + `ClearTeamIconAsync(teamKey)` (+ `TeamManagementService`/`ITeamManagementService` `[RequireScope(TeamScopes.Manage)]` doc entries). Implementation in `TeamServiceBase` calls `IIconStore` then persists the reference; on replace/clear deletes the previous blob.
  - Mongo team repo: `SetIconAsync(teamKey, reference)` (mirror `RenameAsync`), and expose current `Icon` for old-blob cleanup.
  - `AuthorizationTeamServiceDecorator`: gate both on `team:manage` (own team). `AuditingTeamServiceDecorator`: `icon.set` (metadata: content-type, size) / `icon.clear`. New `AuditMetadataKeys` as needed.
  - URL path: `SetTeamIconFromUrlAsync(teamKey, url)` — download server-side via injected `HttpClient` (size-capped stream), then the same save path. (team:manage gated; note SSRF is bounded by the admin scope — document.)
  - Tests in `Tharga.Team.Service.Tests`: auth matrix, audit entries, old-blob deletion on replace/clear, URL download happy/oversize.

- [ ] **4. Registration, options, endpoint (`Tharga.Team.Blazor`)**
  - `ThargaPlatformOptions.AddIconStore<T>()`, `AddIconSource<T>()` (list — multiple, ordered), and `IconOptions` config; register in `ThargaPlatformRegistration`. Always register `IIconResolver` + built-in `StoredIconSource` (last, so custom sources take precedence); custom store via `AddScoped`. Register a named `HttpClient` for URL downloads.
  - `UseThargaPlatform`: map `GET /_tharga/icon/{reference}` → `IIconStore.LoadAsync` → `Results.File(bytes, contentType)` with cache headers; 404 on null; require authenticated user.
  - Tests: endpoint returns bytes/404; `AddIconStore<T>` / `AddIconSource<T>` overrides resolve and order correctly; default store resolves to `MongoIconStore` when the Mongo repo is registered.

- [ ] **5. UI (`Tharga.Team.Blazor`)**
  - `<TeamAvatar>` component: builds an `IconSubject` from the team and resolves via `IIconResolver` → renders `<img src>` (endpoint URL or a custom source's URL) or an initials badge fallback. `TeamInitials` pure helper (tested).
  - `TeamComponent`: show the team avatar; add a "Set icon" control (file upload + URL field) and "Remove icon", gated by `team:manage`; call the service; refresh on success; notifications on error.
  - `TeamsListView` (admin): show `<TeamAvatar>` in the team row.
  - Tests: `TeamInitials`/resolution pure-function tests (repo convention — no bUnit).

- [ ] **6. Sample (`Tharga.Platform.Sample`)** — built-in store auto-registers via existing `AddThargaTeamRepository`; verify a team icon can be set (upload + URL) and renders. Manual smoke test.

- [ ] **7. Full build + test pass** — `dotnet build -c Release && dotnet test -c Release`; push branch for user testing; ask for feedback. Do NOT open the PR yet.

- [ ] **8. Docs (`docs:` commit, before close-out)** — new `docs/articles/icons.md` (or a section): `IIconStore` + built-in Mongo store + pluggability (Azure Blob as the example alternative package), team-icon set/clear, the serving endpoint, size/type limits. Touch README (package list note) + `Tharga.Team.MongoDB/README.md`.

- [ ] **9. Close-out (after user confirms done)** — re-run `dotnet outdated`; archive `feature.md` to the planned spec's context / `done/team-icons.md`; mark Phase A done in the planned spec; `git rm -r plan`; final commit `feat: team-icons complete`; push; open PR → `master` (release-note description; suggest minor bump).

## Notes / decisions

- 2026-07-24: Phase A started off master (post-#133 merge) on `feature/team-icons`. NuGet clean.
- **Built-in store registers from the MongoDB package**, not Blazor — Blazor/core don't reference MongoDB. `TryAdd` so a consumer store set via `o.AddIconStore<T>()` wins (watch cross-package registration order; the platform-options store is applied in `AddThargaPlatform`, the Mongo default in `AddThargaTeamRepository` — verify the override holds regardless of call order, else register the default with lowest priority).
- **Bytes in their own collection**, keyed by reference — never inlined into the hot `Team`/`User` documents.
- `IconKind.User` is defined now but unused until Phase B (avoids a core change later).
- Azure Blob package deferred (out of Phase A scope); the interface is designed so it drops in without core changes.

## Last session

Phase A set up: branch `feature/team-icons` from master, NuGet verified clean, `plan/feature.md` + `plan/plan.md` written. **Awaiting plan confirmation before any code (step 1).**

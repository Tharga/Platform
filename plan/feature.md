# Feature: team-icons (Phase A of team & user icons)

Phase A of the planned "Team & user icons" feature (`$DOC_ROOT/Tharga/plans/Toolkit/Platform/planned/01-team-and-user-icons.md`). It delivers **team icons** end to end and builds the **shared, pluggable `IIconStore` foundation** that Phase B (user icons) and Phase C (store-in-Entra) will reuse.

## Goal

Establish the platform's shared icon foundation — used by **both team and user icons** — and deliver **team icons** on top of it. The foundation has two pluggable seams, each with a sensible built-in default so it works out of the box, and each replaceable by the consuming system:

- **`IIconStore` (storage)** — where icon bytes live. Built-in default: MongoDB (already a dependency, no extra package). Custom: Azure Blob, S3, an existing DMS, etc.
- **`IIconSource` (sourcing)** — where a displayed image comes from. Built-in default: the explicitly-set icon, then an initials fallback. A consuming system registers its own `IIconSource` to source images from its own place.

Phase A deliverable: a team administrator sets a team icon by uploading an image or pointing at an image URL that gets downloaded, and it renders wherever a team is shown, with a graceful initials fallback. **User icons follow in Phase B** (adding Gravatar, federated `picture`, and Entra-photo sources) — no core changes, just more built-in `IIconSource`s.

## Background (current state)

- `ITeam.Icon` / `TeamEntityBase.Icon` already exist (a `string`, `[BsonIgnoreIfNull]`) but nothing sets or renders them — a dormant field ready to hold an icon reference.
- No icon storage exists. Teams and users render only via email-keyed `RadzenGravatar`.
- The MongoDB package registers repositories/collections through `AddThargaTeamRepository` / `ThargaTeamOptions` — the seam where the built-in icon store belongs (Blazor/core don't depend on MongoDB, so the default must register from the MongoDB package).
- The team-service authorization + audit decorators (`AuthorizationTeamServiceDecorator`, `AuditingTeamServiceDecorator`) are the pattern for gating/auditing the new set/clear operations (`team:manage`).

## Scope

1. **Icon abstractions (`Tharga.Team`)** — two pluggable seams:
   - **`IIconStore` (storage)** — `SaveAsync(kind, ownerKey, bytes, contentType) → reference`, `LoadAsync(reference) → IconContent?`, `DeleteAsync(reference)`; `IconKind` (Team, User — User is unused in Phase A but defined so Phase B needs no core change), `IconContent` record, `IconOptions` (max bytes, allowed content types). Consumer override via `o.AddIconStore<T>()`.
   - **`IIconSource` (sourcing)** — `ResolveAsync(IconSubject) → IconImage?` (return null to defer to the next source); `IconSubject` (kind, key, name, stored icon reference; plus email/directory id for Phase B users), `IconImage` (a display URL). An `IIconResolver` composes registered sources in order and falls back to initials. Built-in `StoredIconSource` (serves an explicitly-set icon). Consumer sources via `o.AddIconSource<T>()`, so a host can source images from its own system.
2. **Built-in `MongoIconStore` (`Tharga.Team.MongoDB`)** — an `IconEntity` + `IconRepositoryCollection` (own collection, default name `"Icon"`; bytes stored there keyed by id, **not** inlined into team/user docs) and a `MongoIconStore : IIconStore`. Auto-registered by `AddThargaTeamRepository` via `TryAdd`, so a consumer-supplied store wins. Size/content-type limits enforced.
3. **Authorized, audited team-icon operations** — `SetTeamIconAsync` / `ClearTeamIconAsync` on the team service (persist the reference on `ITeam.Icon`; delete the old blob on replace/clear). Gated by `team:manage` (authorization decorator) and audited (`icon.set` / `icon.clear`). URL-supplied icons are downloaded server-side and passed to the same save path.
4. **Icon-serving endpoint** — `GET /_tharga/icon/{reference}` in `UseThargaPlatform`, streaming bytes from `IIconStore` with content-type + caching headers, for authenticated callers.
5. **`<TeamAvatar>` rendering** — resolves a team's image through `IIconResolver` (registered `IIconSource`s → initials/default fallback); used in the teams admin list and the team management component. Set/clear UI in `TeamComponent`, gated by `team:manage`.
6. **Sample + docs** — the sample's existing `AddThargaTeamRepository` picks up the built-in store automatically; add a set-icon UI path and show the avatar. Update README + `docs/articles/`.

## Out of scope (Phase A)

- User icons, federated `picture` capture, Entra Graph photo, store-in-Entra (Phases B/C).
- The optional `Tharga.Team.Azure` blob-store package — may be scaffolded as a stretch to prove pluggability, otherwise a later follow-up.
- Image cropping/resizing UI; group/role icons.

## Acceptance criteria

- [ ] `IIconStore` exists in core with `o.AddIconStore<T>()`; when unset, the MongoDB build registers `MongoIconStore` as the default (consumer override wins).
- [ ] `IIconSource` exists in core with `o.AddIconSource<T>()`; the built-in `StoredIconSource` + initials fallback resolve an image with no host code; a registered custom source participates in resolution and can override/supply images. `IconKind.User` and the user fields on `IconSubject` are present so Phase B adds sources without a core change.
- [ ] `MongoIconStore` saves/loads/deletes icon bytes in its own collection, keyed by reference; enforces the configured max size and allowed content types; team/user documents are not fattened with binary.
- [ ] A `team:manage` holder can set a team icon by upload or by URL (downloaded server-side); replacing or clearing deletes the previous blob; both are audited (`icon.set` / `icon.clear`); callers without `team:manage` are denied at the service layer.
- [ ] `GET /_tharga/icon/{reference}` streams the icon to authenticated callers with correct content-type and cache headers; unknown reference → 404.
- [ ] `<TeamAvatar>` shows the icon where teams are listed/managed, with an initials fallback when none is set; no directory/federation dependency.
- [ ] Unit tests cover the store (save/load/delete, size/type limits), the set/clear operations (authorization + audit + old-blob cleanup), the URL-download path, and the avatar fallback logic. Full suite green.
- [ ] README and `docs/articles/` updated (icon storage, pluggability, the built-in Mongo store, team-icon usage).

## Done condition

All acceptance criteria met, full test suite passes, user has tested from the pushed branch and confirmed, docs committed, `plan/` removed in the close-out commit, PR from `feature/team-icons` → `master` with release-note-level description. Suggest a minor version bump (additive API + new built-in store).

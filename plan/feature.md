# Feature: per-key-audit-dialog

**Originating branch:** master
**Date started:** 2026-04-29

## Goal

Three connected pieces:

1. Stable per-API-key identifier on every audit entry
2. Reusable `PinnedFilter` parameter on `<AuditLogView />` so a parent can lock specific filter dimensions to a fixed value
3. Row-level "View audit log" button on `<ApiKeyView />` and `<SystemApiKeyView />` that opens an `<AuditLogView />` dialog pinned to the clicked key

End result: a Quilt4Net Server admin can click an API-key row and immediately see what *that specific key* has been doing, without juggling filters in `/developer/audit`.

## Scope

### Part A: stable caller-key identifier (Tharga.Team.Service)

- New `string CallerKeyId { get; init; }` on `AuditEntry`. Holds `IApiKey.Key` (a Guid string) when the caller authenticated with an API key; null otherwise.
- New constant `TeamClaimTypes.ApiKeyId = "ApiKeyId"`. `ApiKeyAuthenticationHandler` adds this claim on successful authentication for both team and system keys.
- `AuditHelper.BuildEntry` reads the claim and populates `CallerKeyId`.
- `ApiKeyAuthenticationHandler.LogAuthEvent` populates it directly from the resolved `IApiKey.Key`.
- `AuditEntryEntity` (Mongo storage) and `MongoDbAuditLogger` round-trip and filter on the new field.
- New filter on `AuditQuery`: `string CallerKeyId { get; init; }`. Mongo logger applies `Eq` filter when present.

### Part B: PinnedFilter on AuditLogView (Tharga.Team.Blazor)

- New public record `AuditPinnedFilter` in `Tharga.Team.Blazor.Features.Audit`:
  ```csharp
  public sealed record AuditPinnedFilter
  {
      public string CallerKeyId { get; init; }
      public AuditCallerType? CallerType { get; init; }
      public string TeamKey { get; init; }
      public string CallerIdentity { get; init; }
      public string Feature { get; init; }
      public string Action { get; init; }
  }
  ```
  Only includes the filters worth pinning right now; consumers can extend later by request.
- `[Parameter] public AuditPinnedFilter PinnedFilter { get; set; }`
- When set, the corresponding top-bar control is **rendered disabled** with the pinned value selected, and the value is **forced into the underlying `AuditQuery`** regardless of any `_filterX` collection state (so it can't be bypassed).
- "Clear all filters" leaves pinned filters in place. (No explicit clear-all button exists today; the request just says pinned filters must not be cleared. We'll honor it if/when one's added.)
- Export respects the pinned filter — already true since export builds via `BuildQuery`.

### Part C: dialog launch from ApiKeyView / SystemApiKeyView (Tharga.Team.Blazor)

- New `[Parameter] public bool ShowAuditLogButton { get; set; }` on `ApiKeyView` and `SystemApiKeyView`. Default `false` so existing consumers see no UI change. Set to `true` on a per-page basis to opt in.
- When the parameter is `true`, render a row-level button (icon `manage_search`) on each `<RadzenDataGrid>` row.
- Click opens a `DialogService.OpenAsync` hosting `<AuditLogView PinnedFilter="..." />`. Pin:
  - `CallerKeyId = <row's key.Key>`
  - `CallerType = AuditCallerType.ApiKey`
- Dialog title: `Audit log — {key.Name}`.
- Wide dialog (e.g. `Width = "min(1100px, 95vw)"`) so the audit grid has room.

### Out of scope

- Audit-for-team / audit-for-user dialogs (filed separately if needed; the same `AuditPinnedFilter` primitive will support them).
- New filter field types or export formats.
- Backfill of `CallerKeyId` on existing audit entries — null for legacy entries is fine; new entries get it going forward.

## Acceptance criteria

- [ ] `AuditEntry.CallerKeyId` populated by both the auth handler and `AuditHelper`
- [ ] `TeamClaimTypes.ApiKeyId` constant added; the auth handler emits the claim for team and system keys
- [ ] `AuditQuery.CallerKeyId` filter applied by `MongoDbAuditLogger`
- [ ] `AuditPinnedFilter` record + `<AuditLogView PinnedFilter=...>` parameter
- [ ] When `PinnedFilter` is set, the matching top-bar controls render disabled with the pinned values
- [ ] When `PinnedFilter` is set, `BuildQuery` forces the pinned values into the resulting `AuditQuery` (overriding any local state)
- [ ] `ApiKeyView` and `SystemApiKeyView` each gain a "View audit log" button that opens the dialog
- [ ] All existing tests pass; new tests cover the auth-claim/audit-entry round-trip, the query filter, and the AuditLogView pinned-filter override behavior

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete.

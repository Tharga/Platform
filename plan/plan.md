# Plan: per-key-audit-dialog

## Steps

### Part A: stable caller-key identifier
1. [x] Add `CallerKeyId` to `AuditEntry`
2. [x] Add `TeamClaimTypes.ApiKeyId = "ApiKeyId"` constant
3. [x] Update `ApiKeyAuthenticationHandler` to emit the claim and populate `LogAuthEvent`
4. [x] Update `AuditHelper.BuildEntry` to read the claim
5. [x] Add `CallerKeyId` to `AuditEntryEntity` (Mongo) and round-trip in `ToAuditEntry` / `ToEntity`
6. [x] Add `CallerKeyId` to `AuditQuery` and apply it as an `Eq` filter in `MongoDbAuditLogger.QueryAsync`

### Part B: PinnedFilter on AuditLogView
7. [x] Create `AuditPinnedFilter.cs` (public record) in `Tharga.Team.Blazor.Features.Audit`
8. [x] Add `[Parameter] public AuditPinnedFilter PinnedFilter { get; set; }` to `AuditLogView`
9. [x] On `OnInitializedAsync`, seed `_filterX` state from `PinnedFilter` when fields are set
10. [x] In the markup, disable the matching top-bar controls when `PinnedFilter.<Field>` is non-null
11. [x] In `BuildQuery`, force pinned values into the result regardless of the `_filterX` state

### Part C: dialog launch
12. [x] Add a row-level "View audit log" button to `ApiKeyView.razor` (regular API keys, pinned `CallerKeyId` + `CallerType=ApiKey`)
13. [x] Same on `SystemApiKeyView.razor`
14. [x] Open via `DialogService.OpenAsync` with a wide responsive dialog hosting `<AuditLogView PinnedFilter=... />`
15. [x] Dialog title shows the key's Name

### Tests
16. [x] Auth handler — emits `ApiKeyId` claim on success (team key + system key)
17. [x] Auth handler — `LogAuthEvent` writes `CallerKeyId`
18. [x] AuditHelper — picks up `CallerKeyId` from claim
19. [x] AuditQuery — `CallerKeyId` filter exposed (smoke check via record equality)
20. [x] AuditLogView — `BuildQuery` forces the pinned values (small unit test on `BuildQuery` if reachable, otherwise smoke check on the parameter shape)
21. [x] Compile-time test: `AuditPinnedFilter` is public, has expected fields

### Verify & ship
22. [x] Full build + test suite passes — 263 tests (8 new)
23. [ ] Archive plan, delete plan/, final commit, push, PR

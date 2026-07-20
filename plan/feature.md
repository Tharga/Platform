# Feature: Audit metadata (#129)

## Goal

Make audit entries record **what changed**, not just that something happened — and give hosts a way to
attach their own metadata to entries the toolkit writes.

## Background — `Metadata` exists but is vestigial

`AuditEntry.Metadata` (`Dictionary<string, string>`, commented "Extensibility") already exists. Today:

| Layer | State |
|---|---|
| `AuditEntry` / `AuditEntryEntity` | field exists |
| `MongoDbAuditLogger` | round-trips it (`:206`, `:231`) |
| `LoggerAuditLogger` | **drops it** — absent from the log template |
| `AuditLogView` grid | **never renders it** |
| CSV export | **omits it** (hard-coded columns, `AuditLogView.razor.cs:379-380`) |
| JSON export | already included (whole-object serialization, `:370`) |
| `AuditingTeamServiceDecorator` | **never populates it** |
| `AuditingApiKeyServiceDecorator` | one usage: `{"ApiKeyType": "System"}` (`:222`) |
| Consumer hook | **none** |

One writer, and its output is invisible in the UI and silently discarded whenever `StorageMode` is
`Logger` — the default the audit docs already flag as a gotcha.

## Scope

1. **Built-in operations populate metadata.** Thread a `metadata` argument through
   `AuditingTeamServiceDecorator.Log` → `AuditHelper.BuildEntry` (and the API-key decorator), then
   populate per operation: create → team name; rename → old + new; member invite/remove/role/display-name
   → member and before/after; consent → old + new level and roles; custom-role CRUD → role name and scopes.
2. **`IAuditEnricher` consumer hook.** Invoked in `CompositeAuditLogger.Log` — the single funnel every
   entry passes through — before dispatch to the loggers. Registered via options, mirroring the existing
   `ITeamClaimsEnricher` / `AddClaimsEnricher<T>()` pattern.
3. **Close the visibility gaps** so any of the above is observable: `LoggerAuditLogger` output, the
   `AuditLogView` grid, and CSV export. Verify JSON export still carries it.
4. **Revive `ConsentAuditWiringTests`** from `origin/investigate/consent-audit` (commit `730f188`) —
   a regression guard that `ITeamService` resolves audit-decorated and that a consent change reaches the
   sink. Guards the #87 failure mode (registration order silently clobbering the decorator). Needs one
   added override (`SetTeamCustomRolesInternalAsync` became abstract after the commit was written).

## Non-goals

- Auditing team enumeration (`GetAllTeamsAsync`) — deliberately unaudited, decided in 3.2.0.
- Letting an enricher suppress an entry. Filtering is `AuditOptions`' job; an enricher adds context.
- Changing the audit storage schema beyond what `Metadata` already supports.

## Acceptance criteria

- [ ] Create, rename, member and consent operations record their own before/after detail in `Metadata`.
- [ ] A host can register an `IAuditEnricher` and see its values on entries the toolkit writes.
- [ ] Enricher failures cannot break an audited operation (an audit sink must never take down the call).
- [ ] Metadata is visible in `AuditLogView` and included in CSV export; JSON export unchanged.
- [ ] `LoggerAuditLogger` no longer silently discards metadata.
- [ ] `ConsentAuditWiringTests` compiles, passes, and is part of the suite.
- [ ] Existing entries with `Metadata == null` render and export without error (back-compat).
- [ ] Full suite green; `dotnet build -c Release` clean.
- [ ] `README.md` and `docs/articles/implementation-guide.md` updated.

## Open questions (need answers before steps 4 and 6)

1. **Grid rendering shape** — expandable detail row, details dialog (per-key audit dialog from #57 is
   the in-repo precedent), or a truncated summary column with detail on hover?
2. **How far before/after capture goes** — every audited operation, or only where the old value is
   genuinely useful? Each one costs a read before the mutation.

## Done condition

All acceptance criteria met, user has tested from the pushed branch and confirmed, then close-out per
the Feature Workflow.

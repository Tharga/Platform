# Feature: Audit log error details on the "OK" column

## Goal
When an audit entry represents a failure, let the reader see *why* it failed directly from
the `AuditLogView` "OK" column — without opening the export. Bundled with the mandatory
up-front dependency step (SixLabors.ImageSharp 3.1.12 → 4.0.0).

## Background
`AuditEntry` already carries the failure detail; it is simply not surfaced in the grid today:
- `ErrorMessage` — the reason text (currently only in CSV/JSON export).
- `EventType` (`ScopeDenial` / `AccessLevelDenial` / `AuthFailure` / `RateLimit` / `ServiceCall`) — the failure classification, used here as the "response code".
- `ScopeResult` + `ScopeChecked` — authorization outcome, part of the detail.

The "OK" column currently renders a static green `check_circle` / red `error` icon with no
tooltip and is not interactive.

## Scope
- **In:** OK column shows, for failures, the red icon **+ a short code (EventType) to its right**, plus a **detailed hover tooltip** (Event, Scope + result, Reason). Success stays a plain green check.
- **In:** an `internal static` helper on `AuditLogView` that derives the short code and the detailed tooltip text from an `AuditEntry`, unit-tested in `Tharga.Team.Blazor.Tests`.
- **In:** SixLabors.ImageSharp 3.1.12 → 4.0.0 bump (up-front), verified by build + the 4 existing `ImageSharpIconProcessorTests`.
- **Out:** no `AuditEntry` schema change (no new HTTP-style numeric code — audit calls are not HTTP). No change to export formats (ErrorMessage already exported). No new filter.

## Acceptance criteria
- [ ] SixLabors.ImageSharp at 4.0.0; solution builds `-c Release`; Images tests green.
- [ ] Failed audit rows show the failure code next to the red icon and a detailed tooltip with the reason; successful rows are unchanged (green check).
- [ ] The code/tooltip derivation is a pure helper with unit tests covering success, scope denial, access-level denial, auth failure, and a plain exception (no ErrorMessage).
- [ ] Full test suite green (`dotnet test -c Release`).
- [ ] Tharga.Team.Blazor README + docs audit section updated.

## Done condition
All acceptance criteria met, user has tested from the pushed branch and confirmed, docs
committed, `plan/` removed in the close-out commit, PR opened to master.

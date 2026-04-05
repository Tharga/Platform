# Plan: audit-builtin-operations

## Steps

1. [x] Create `AuditingTeamServiceDecorator` in `Tharga.Team.Service` — wraps `ITeamService`, logs mutations via `CompositeAuditLogger`
2. [x] Create `AuditingApiKeyServiceDecorator` in `Tharga.Team.Service` — wraps `IApiKeyAdministrationService`, logs mutations
3. [x] Register decorators conditionally in `Tharga.Team.Blazor` — only when `CompositeAuditLogger` is available
4. [x] Created shared `AuditHelper` for building audit entries from HTTP context
5. [x] Write tests for team audit decorator — 15 tests (12 mutations + reads + failure + event type)
6. [x] Write tests for API key audit decorator — 6 tests (4 mutations + read + failure)
7. [x] Run full test suite — 189 tests pass (120 service + 69 blazor)
8. [ ] Commit

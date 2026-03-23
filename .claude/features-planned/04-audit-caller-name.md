# Feature: AuditLogView Should Show Caller Name, Not ID

**Requested by:** Daniel Bohlin
**Date:** 2026-03-23

## Goal
Show human-readable caller names in AuditLogView instead of raw user IDs or API key identifiers.

## Changes

### Resolve caller identity to display name
- For **web/user calls**: resolve and display the user's display name (from `IUserService` or the `name`/`preferred_username` claim)
- For **API key calls**: display the API key's name (from `IApiKeyAdministrationService`)
- The raw ID should still be available (e.g. as a tooltip or secondary column) for debugging

## Acceptance Criteria
- [ ] AuditLogView shows caller display name as primary identifier
- [ ] User calls show the user's display name
- [ ] API key calls show the API key's name
- [ ] Raw ID is available via tooltip or secondary column
- [ ] Graceful fallback to raw ID when name resolution fails

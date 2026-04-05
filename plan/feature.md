# Feature: audit-builtin-operations

**Originating branch:** develop
**Date started:** 2026-04-05

## Goal

Add audit logging for Platform's built-in team and API key operations by injecting optional `IAuditLogger` into `TeamServiceBase` and `ApiKeyAdministrationService`.

## Scope

- Inject optional `CompositeAuditLogger` into `TeamServiceBase` — log all mutation methods
- Inject optional `CompositeAuditLogger` into `ApiKeyAdministrationService` — log all mutation methods
- Log entries include: feature, action, caller identity, team key, duration, success/failure
- When `IAuditLogger` is not registered (no `AddThargaAuditLogging()` called), no logging happens — zero overhead
- When teams are not configured, TeamKey is null in audit entries — still valid

## Operations to audit

### TeamServiceBase
- CreateTeamAsync
- RenameTeamAsync
- DeleteTeamAsync
- AddMemberAsync
- RemoveMemberAsync
- SetMemberRoleAsync
- SetMemberTenantRolesAsync
- SetMemberScopeOverridesAsync
- SetInvitationResponseAsync (accept and reject)
- SetTeamConsentAsync
- TransferOwnershipAsync

### ApiKeyAdministrationService
- CreateKeyAsync
- RefreshKeyAsync
- LockKeyAsync
- DeleteKeyAsync

## Acceptance criteria

- [ ] All team mutation methods log audit entries when `IAuditLogger` is registered
- [ ] All API key mutation methods log audit entries when `IAuditLogger` is registered
- [ ] No logging or errors when `IAuditLogger` is not registered
- [ ] Audit entries have correct feature/action, caller identity, and team key
- [ ] Tests verify logging for each operation
- [ ] Tests verify no-op when audit is not configured
- [ ] All existing tests pass

## Done condition

All acceptance criteria met, all tests pass, user confirms feature is complete.

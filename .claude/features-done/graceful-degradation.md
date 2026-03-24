# Feature: Graceful Degradation for All Components

## Goal
All Blazor components that inject optional/conditional services should degrade gracefully with clear in-page error messages instead of crashing when a required `Add*` method hasn't been called.

## Originating branch
develop

## Scope
Two components need changes:
1. **AuditLogView** — injects `CompositeAuditLogger` via `[Inject]`. Crashes if `AddThargaAuditLogging()` not called. Should use `IServiceProvider.GetService` and show a message.
2. **ApiKeyView** — injects `IApiKeyManagementService` via `@inject`. Crashes if the service isn't registered. Should use `IServiceProvider.GetService` and show a message.

Other components (TeamComponent, TeamSelector, UsersView, etc.) inject services registered by `AddThargaTeamBlazor()` — these are always present when the component is reachable, so no change needed.

## Acceptance Criteria
- [ ] AuditLogView renders a clear message when CompositeAuditLogger is not registered
- [ ] ApiKeyView renders a clear message when IApiKeyManagementService is not registered
- [ ] No unhandled exceptions from missing DI registrations
- [ ] Tests verify both "service registered" and "service missing" paths
- [ ] Existing functionality unchanged when services are registered

## Done Condition
All acceptance criteria met, all tests pass.

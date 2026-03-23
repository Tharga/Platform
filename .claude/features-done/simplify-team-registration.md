# Feature: Simplify Team Registration & Fix Missing Service Registrations

## Goal
Fix missing service registrations that cause crashes or hidden UI, making Team Management work out of the box.

## Originating branch
develop

## Scope
1. Add `RegisterTeamService<TService, TUser, TMember>()` overload — stores `_memberType`
2. `AddThargaTeamBlazor` registers `ITeamManagementService` when `_memberType` is set
3. `AddThargaApiKeys` registers `IApiKeyManagementService`
4. `AddThargaTeamBlazor` auto-registers default TeamScopes + ApiKeyScopes when team service is configured
5. Update `AddThargaPlatform` to use the new 3-param overload
6. Tests and docs

## Acceptance Criteria
- [ ] `RegisterTeamService<T,U,M>()` stores member type and auto-registers ITeamManagementService
- [ ] `AddThargaApiKeys()` registers IApiKeyManagementService
- [ ] Default TeamScopes auto-registered when team service is configured
- [ ] Existing 2-param `RegisterTeamService` still works (no ITeamManagementService registered)
- [ ] AddThargaPlatform updated
- [ ] All tests pass

## Done Condition
All acceptance criteria met, user confirms done.

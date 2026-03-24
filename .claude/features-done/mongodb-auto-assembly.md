# Feature: MongoDB Auto-Assembly Registration

## Goal
Platform packages explicitly register their MongoDB repository types so consumers with non-Tharga name prefixes don't need manual AddAutoRegistrationAssembly calls.

## Originating branch
develop

## Scope
1. `AddThargaApiKeys()` — explicitly register `IApiKeyRepository → ApiKeyRepository` and `IApiKeyRepositoryCollection → ApiKeyRepositoryCollection`
2. `AddThargaTeamRepository()` — already registers its types explicitly (no change needed)
3. `AddThargaAuditLogging()` — already registers `IAuditRepositoryCollection` explicitly (no change needed)

## Acceptance Criteria
- [ ] AddThargaApiKeys registers IApiKeyRepository and IApiKeyRepositoryCollection
- [ ] Tests verify the services resolve correctly
- [ ] Existing consumers unaffected (no breaking change)

## Done Condition
All acceptance criteria met, all tests pass.

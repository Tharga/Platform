# Feature: Team-bound service authorization — phase 1

Phase 1 of `planned/01-team-bound-service-authorization.md`: classification APIs, interface splits,
and a single authorization brain. Phases 2–4 follow separately.

## Goal

A scope must be checked against the team named in the call, and a service must not be able to reach the
database with no check at all. Phase 1 lays the structure that makes that possible: services declare at
registration whether they are team-bound or system-wide, and the interfaces are made homogeneous so the
declaration can be true of every method.

## Background

Scope enforcement has three paths today and only one of them works:

- `AuthorizationTeamServiceDecorator` binds the scope to the target team via `TeamAuthorizer` — correct.
- `ScopeProxy` checks that *a* team is selected and the scope claim exists *somewhere* — unbound.
- Plain `AddScoped` — no check at all. This is the live state of `IApiKeyManagementService`
  (`ControllersRegistration.cs:88`); `AddScopedWithScopes` has no product call sites, so `ScopeProxy`
  never wraps it and its `[RequireScope]` attributes are inert.

The categories already exist informally, as comments in `AuthorizationTeamServiceDecorator`
(`// Reads / self-service`, `// Lifecycle.`, `// Team administration`). This makes them types.

## Scope (phase 1)

- `AddTeamService` / `AddSystemService` registration APIs; the choice selects the decorator.
- Split `IApiKeyManagementService`: seven `teamKey` methods stay, five `*SystemKeyAsync` move to a new
  `ISystemApiKeyManagementService`.
- Move `CreateTeamAsync` off `ITeamManagementService` — no team exists yet and its real rule is
  "authenticated + `AllowTeamCreation`".
- `ScopeProxy` authorizes via `TeamAuthorizer` rather than its own claim inspection.

Deferred to later phases: two-way registration validation and the startup sweep (phase 2), the UI gate
helper (phase 3), the database access guard (phase 4, blocked on a Tharga.MongoDB seam requested
2026-07-25).

## Acceptance criteria

- [ ] A team service call is rejected unless the caller holds the scope **for the team named in that
      call's own argument** — proven by a test passing team B while holding the scope for team A.
- [ ] A system service call succeeds with no team selected (today `CheckScope` wrongly requires one).
- [ ] `IApiKeyManagementService` split; `CreateTeamAsync` relocated; the four services classified.
- [ ] `ScopeProxy` performs no claim inspection of its own.
- [ ] Full test suite passes.

## Done condition

Phase 1 merged, with the migration for consumers spelled out in the PR description. Phase 2 can start.

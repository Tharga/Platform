# Plan: Team-bound service authorization — phase 1

Branch: `feature/team-bound-service-authorization` (from `master`, which now carries PR #141)

## Steps

- [x] 1. NuGet package pass (mandatory feature-start step)
  - `dotnet outdated`: only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, deliberately held (4.0 requires a paid
    Six Labors build-time licence). Nothing else outstanding.

- [x] 2. Baseline build + test — clean build, 920 tests pass.

- [x] 3. One authorization brain, and the team binding
  - `TeamAuthorizer` couldn't be injected into `ScopeProxy` directly: `ProxyInvoker` hands enforcement a
    *synchronous* `Action<ClaimsPrincipal>`, while `TeamAuthorizer` is async and resolves the principal
    itself. Extracted the decision instead — `TeamScopePolicy` (pure, principal-in) is now the single
    implementation, with `TeamAuthorizer` as its async wrapper and `ScopeProxy` calling it directly.
  - `ServiceScopeKind` (Team/System) supplied at construction. `ScopeProxy` resolves the **target** team
    from the call's first argument and requires the scope for *that* team — closing the hole where a
    scope held for team A authorized acting on team B.
  - `scopeKind` is deliberately a **required** parameter, not defaulted. A default picks an authorization
    policy on the caller's behalf, which is the failure this feature exists to prevent.
  - `AddTeamService` / `AddSystemService` added over `AddScopedWithScopes`, which now takes the kind.
  - Existing proxy tests used parameterless fixtures, which under the new model name no team. Gave the
    two team fixtures a `teamKey` first argument (preserving `Circuit_Without_TeamKey_Denies`'s intent)
    and added a system fixture. +5 tests including the load-bearing A-vs-B rejection. Suite: 925 pass.

- [x] 4. Split `IApiKeyManagementService`
  - Five `*SystemKeyAsync` methods moved to a new `ISystemApiKeyManagementService`, with a matching
    `SystemApiKeyManagementService` implementation. They were pure delegations to
    `IApiKeyAdministrationService` with no per-team owner-scoping, so the seam was clean.
  - `SystemApiKeyView` was the only affected call site.

- [x] 5. Relocate `CreateTeamAsync`
  - New `ITeamLifecycleService`. `TeamManagementService<TMember>` implements both interfaces (one class,
    two registrations) rather than splitting the implementation — the method is a single delegation and a
    second class would be ceremony.
  - `TeamComponent` injects `ITeamLifecycleService` for the create path.

- [x] 6. `AddTeamService` / `AddSystemService`
  - `AddThargaApiKeys` now registers the team service and the system service by kind, replacing the plain
    `AddScoped` that installed no wrapper at all. **This is the step that closes the live hole** — the
    binding built in step 3 now actually applies to API key management.

- [x] 7. Tests
  - `ScopeServiceRegistrationTests` (7): the registration APIs through a real container — team B rejected
    while holding team A's scope, team A allowed, resolved instance is a wrapper not the implementation,
    system service works with no team selected, and `AddThargaApiKeys` registers via a factory (the shape
    a plain `AddScoped` cannot produce) with both interfaces present.
  - Suite: 932 pass (baseline 920).

- [ ] 8. Full suite, then docs review
  - The registration APIs and the interface split are consumer-facing; check `README.md` and
    `docs/articles/` for content on service registration and scopes.

- [ ] 9. Close out
  - Re-run `dotnet outdated`, archive `plan/feature.md`, remove `plan/`, final commit, push, PR with the
    consumer migration spelled out.

## Notes

- Master already contains PR #141, so this branches cleanly with no rebase pending.
- Breaking for consumers: `IApiKeyManagementService` loses five methods, `ITeamManagementService` loses
  `CreateTeamAsync`, and anyone registering their own `[RequireScope]` service moves to the new APIs.
  Warrants a minor version bump.

## Last session

2026-07-26 — Steps 1–7 complete; phase 1's substance is done and the live hole is closed. 932 tests pass
(baseline 920). Remaining: step 8 (docs review — the registration APIs and both interface splits are
consumer-facing) and step 9 (close out).

Phase 2 (two-way registration validation, startup sweep, architecture test) and phase 3 (UI gate helper)
are unblocked and can follow. Phase 4 waits on the Tharga.MongoDB interception seam, in progress there.

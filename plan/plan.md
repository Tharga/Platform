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

- [~] 4. Split `IApiKeyManagementService`
  - Seven `teamKey`-taking methods stay.
  - `GetSystemKeysAsync`, `CreateSystemKeyAsync`, `RefreshSystemKeyAsync`, `LockSystemKeyAsync`,
    `DeleteSystemKeyAsync` move to a new `ISystemApiKeyManagementService`.
  - Update `SystemApiKeyView` (system) and `ApiKeyView` (team) to the split interfaces; the sample's
    `/api-keys` and `/system-api-keys` pages already separate along this seam.

- [ ] 5. Relocate `CreateTeamAsync`
  - Off `ITeamManagementService` onto a lifecycle service. Authorization stays
    "authenticated + `AllowTeamCreation`" as `RequireCreateAsync` already implements.
  - Call sites: `TeamComponent.CreateTeam`, plus any sample usage.

- [ ] 6. `AddTeamService` / `AddSystemService`
  - Registration APIs that install the matching decorator. Replace the plain
    `AddScoped<IApiKeyManagementService, …>` in `ControllersRegistration.AddThargaApiKeys`.
  - Team registration resolves the team key from the call's first argument (one rule for the whole
    interface — validation of that rule is phase 2).

- [ ] 7. Tests
  - The load-bearing one: holding the scope for team A, call a team service passing team B → rejected.
  - System service with no team selected → allowed.
  - Existing `ScopeProxyTests` / `ScopeProxyPrincipalAccessorTests` updated for the new shape.

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

2026-07-25 — Branch created from master, package pass done (ImageSharp held). Plan written; next is the
baseline build/test at step 2, then the `TeamAuthorizer` consolidation at step 3.

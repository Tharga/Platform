# Plan: Runtime-defined (dynamic) tenant roles per team (#117)

Branch: `feature/dynamic-tenant-roles` (from `master`, GitHub Actions strategy → PR to `master`)

## Steps

- [x] 1. **[Mandatory, first] Package check.** `dotnet outdated` across the solution — no-op
  (MongoDB 2.13.0 already on master from #116). Build/test baseline green.
- [x] 2. **Store.** Done — `CustomRoles` on `ITeam` (default `=> null`) + `TeamEntityBase<T>`
  (`[BsonIgnoreIfNull]`); `SetCustomRolesAsync` on `ITeamRepository`/`TeamRepository`. Serialization
  round-trip test (incl. nested scopes + null-omission), 2 tests green.
- [x] 3. **Service write path.** Done — `GetTeamCustomRolesAsync` + `SetTeamCustomRolesAsync` on
  `ITeamService`/`TeamServiceBase` (+ abstract `SetTeamCustomRolesInternalAsync`), repo bridge,
  `ITeamManagementService`/`TeamManagementService` facade (`[RequireScope(Manage)]`). 3 test doubles
  updated.
- [x] 4. **Auth + audit decorators.** Done — escalation + structural guard in
  `AuthorizationTeamServiceDecorator` (team:manage; scopes ⊆ `IScopeRegistry.All`; non-empty/unique/no
  code-role collision; optional registries injected via the DI factory). Audit `set-custom-roles`.
  9 authorization + 1 audit test green (26 in the two files).
- [x] 5. **Team-aware resolver.** Done — `ITenantRoleService`/`TenantRoleService`
  (`GetRolesAsync`, `GetEffectiveScopesAsync`). 6 unit tests green.
- [x] 6. **Claims wiring.** Done — `TeamServerClaimsTransformation`,
  `TeamClaimsAuthenticationStateProvider`, `ApiKeyAuthenticationHandler` prefer
  `ITenantRoleService.GetEffectiveScopesAsync` (optional ctor param) with null-tolerant fallback to
  `ScopeRegistry.GetEffectiveScopes`. Server-path integration test green.
- [x] 7. **Feature flag + registration.** Done — `ThargaPlatformOptions.EnableDynamicRoles`;
  `AddThargaDynamicTenantRoles()` (`TryAddScoped`) wired conditionally in `ThargaPlatformRegistration`.
  2 registration tests (on registers `ITenantRoleService`, default does not).
- [~] 8. **Picker integration.** `TeamComponent`/`RoleEditor` source roles from
  `ITenantRoleService.GetRolesAsync(team.Key)` so custom roles show alongside code roles (respecting
  `ITenantRoleVisibilityProvider`). bUnit test.
- [ ] 9. **Management component.** `<TenantRoleManager />` — list/create/edit/delete, scope multiselect
  from `IScopeRegistry.All`, `team:manage`-gated, calls `ITeamManagementService`. bUnit tests.
- [ ] 10. **Docs.** `Tharga.Team*/README.md` + `docs/articles/implementation-guide.md` (+ getting-started
  if warranted): the feature, the flag, and the component.
- [ ] 11. **Verify.** Full `dotnet build/test -c Release` green; end-to-end via sample app (enable flag,
  create a custom role, assign to a member, confirm the scope claim is emitted).
- [ ] 12. **Close-out** (only on user confirmation): re-check `dotnet outdated`; archive `plan/feature.md`
  → Plan `done/`; `git rm -r plan`; final `feat: … complete` commit; push; open PR.

## Notes / decisions
- Storage type: reuse `TenantRoleDefinition`. Store embedded on the Team document (mirror `ConsentedRoles`).
- Whole-array setter `SetTeamCustomRolesAsync` (matches `SetConsentAsync`); UI does create/edit/delete on
  the array. Escalation + structural validation in the authorization decorator (registered entry point;
  no `TeamServiceBase` ctor ripple).
- `team:manage` defines roles; `member:manage` assigns them (unchanged).
- Feature default OFF → non-breaking. WASM claims path updated for parity but remains best-effort
  (see the separate auto-hosting-detection limitation).
- `.claude/CLAUDE.md` has an unrelated intentional working-tree edit — kept OUT of this feature's commits.

## Last session
(current) — Branch created, architecture mapped, design confirmed (full management component), plan
written. Next: step 2 (store).

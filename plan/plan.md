# Plan: Runtime-defined (dynamic) tenant roles per team (#117)

Branch: `feature/dynamic-tenant-roles` (from `master`, GitHub Actions strategy → PR to `master`)

## Steps

- [x] 1. **[Mandatory, first] Package check.** `dotnet outdated` across the solution — no-op
  (MongoDB 2.13.0 already on master from #116). Build/test baseline green.
- [~] 2. **Store.** Add `CustomRoles` (`IReadOnlyList<TenantRoleDefinition>`) to `ITeam`
  (default `=> null`) and `TeamEntityBase<T>` (`[BsonIgnoreIfNull]`). Add `SetCustomRolesAsync` to
  `ITeamRepository` + `TeamRepository` (mirror `SetConsentAsync`). Round-trip persistence test.
- [ ] 3. **Service write path.** `ITeamService.GetTeamCustomRolesAsync` + `SetTeamCustomRolesAsync`;
  `TeamServiceBase` impls (event raise + cache) + abstract `SetTeamCustomRolesInternalAsync`;
  `TeamServiceRepositoryBase` bridge → repo; `ITeamManagementService` facade methods
  (`[RequireScope(TeamScopes.Manage)]`).
- [ ] 4. **Auth + audit decorators.** `AuthorizationTeamServiceDecorator.SetTeamCustomRolesAsync`:
  `team:manage` + escalation guard (scopes ⊆ `IScopeRegistry.All`) + structural validation
  (non-empty/unique/no code-role collision). `AuditingTeamServiceDecorator`: `set-custom-roles`.
  Tests for allow/deny/escalation-reject/collision-reject.
- [ ] 5. **Team-aware resolver.** New `ITenantRoleService` + impl: `GetRolesAsync(teamKey)` (merge
  code+custom), `GetEffectiveScopesAsync(teamKey, accessLevel, roleNames, overrides)` (AL ∪ code ∪
  custom ∪ overrides). Unit tests.
- [ ] 6. **Claims wiring.** Switch `TeamServerClaimsTransformation`, `TeamClaimsAuthenticationStateProvider`,
  `ApiKeyAuthenticationHandler` to `GetEffectiveScopesAsync` with null-tolerant fallback. Integration
  test: member with a custom role → custom scope claims.
- [ ] 7. **Feature flag + registration.** `ThargaPlatformOptions.EnableDynamicRoles`; conditional
  `AddThargaDynamicTenantRoles()` in `ThargaPlatformRegistration`; optional/null-tolerant per
  `OptionalRegistryTests`. Registration tests (on/off).
- [ ] 8. **Picker integration.** `TeamComponent`/`RoleEditor` source roles from
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

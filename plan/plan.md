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
- [x] 8. **Picker integration.** Done — `TeamComponent` merges each team's `CustomRoles` (from the
  already-loaded team doc, gated on `ITenantRoleService` present) with code roles in
  `BuildVisibleRolesAsync`, feeding `RoleEditor` and respecting `ITenantRoleVisibilityProvider`; the
  scope-origin tooltip now includes custom-role scopes. (No bUnit in repo — component logic tested via
  extracted classes per codebase convention.)
- [x] 9. **Management component.** Done — `<TenantRoleManager />` (list/add/edit/delete, scope
  multiselect from `IScopeRegistry.All`, `team:manage`-gated, `EnableDynamicRoles`-gated, calls
  `ITeamManagementService.SetTeamCustomRolesAsync`). Logic extracted to `TenantRoleManagerModel`;
  7 unit tests green.
- [x] 10. **Docs.** Done — `Tharga.Team/README.md` (`ITenantRoleService`), `Tharga.Team.Blazor/README.md`
  (component bullet + "Dynamic (runtime-defined) tenant roles" section), and
  `docs/articles/implementation-guide.md` (Step 7 subsection with the flag, gating, and component).
- [x] 11. **Verify.** Done — full suite 537 green; real-composition end-to-end resolution test
  (`TestTeamService` store → `TenantRoleService` resolve → custom scopes). Sample wired
  (`EnableDynamicRoles = true` + `/roles` page + nav link); app boots cleanly (DI resolves), root 200,
  `/roles` 302→login like other authorized pages. Full authenticated click-through needs Azure AD + Mongo
  (user's environment).
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
Steps 1–11 complete. Store + service + audited authz + escalation guard + team-aware resolver +
claims/API-key wiring + feature flag + picker integration + `TenantRoleManager` component + docs +
sample wiring, all done. 537 tests green; sample boots cleanly with the feature on. Awaiting user
test/confirmation before close-out (step 12: re-check outdated, archive feature.md, `git rm -r plan`,
final `feat: … complete` commit, push, PR).

# Plan: member:manage umbrella scope (Tharga/Platform#102)

- [x] 0. NuGet up front — `dotnet outdated`: "No outdated dependencies". No-op.
- [x] 1. `TeamScopes.MemberManage = "member:manage"`
- [x] 2. `ScopeDefinition.Implies` (IReadOnlyList<string>, default null)
- [x] 3. `ScopeRegistry.Register(..., implies = null)` + transitive cycle-safe `ExpandImplied` in `GetEffectiveScopes`
- [x] 4. Registered `member:manage` at Administrator implying invite/remove/role (default block)
- [x] 5. Tests: ImpliedScopesTests (6) + SimplifyRegistrationTests default-registration asserts
- [x] 6. Docs: scopes table row + "Umbrella (implied) scopes" section
- [~] 7. Build + full test run (468/468 green); commit; present for user testing

## Notes
- Confirmed design: general implied-scopes (not hardcoded to member:manage).
- `GetEffectiveScopes` is the single chokepoint (ApiKeyAuthenticationHandler, TeamServerClaimsTransformation,
  TeamClaimsAuthenticationStateProvider, TeamComponent/ApiKeyView UI) → expansion propagates everywhere.
- member scopes are a consumer vocabulary (no library `[RequireScope]` on member ops) — no enforcement change.

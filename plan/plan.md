# Plan: Show system vs team scopes in ScopeView

- [x] 0. NuGet up front — "No outdated dependencies". No-op.
- [x] 1. `ScopeReference.UserSystemScopes(ISystemScopeRegistry, userScopes)` pure helper
- [x] 2. `ScopeView`: resolve ISystemScopeRegistry; principal fetched once; held system scopes from claims
- [x] 3. `ScopeView`: render Team scopes (existing) + System scopes table (held only) + headings; `ShowSystemScopes` (default true)
- [x] 4. Tests: ScopeReferenceTests (+5 UserSystemScopes); ScopeViewTests (+ShowSystemScopes param)
- [x] 5. Docs: ScopeView tip extended with the System scopes table
- [~] 6. Build + full test run (468/468 green); commit; present

## Notes
- System scopes reach the user's claims via TeamServerClaimsTransformation (system-role → Scope claim).
- Distinguish via registry membership: claim ∈ ISystemScopeRegistry → system; ∈ IScopeRegistry → team.
- Refactor OnInitializedAsync to fetch the principal once (used for team defaults + system scopes).

# Plan: Remove granular member scopes (Tharga/Platform)

- [x] 0. NuGet up front — `dotnet outdated`: "No outdated dependencies". No-op.
- [x] 1. TeamScopes: removed MemberInvite/Remove/Role; kept MemberManage (doc updated)
- [x] 2. Reverted implied-scopes: ScopeDefinition.Implies, Register(implies:), ExpandImplied
- [x] 3. ThargaBlazorRegistration: dropped 3 granular Register calls; member:manage plain
- [x] 4. ITeamManagementService: 5 [RequireScope] → MemberManage
- [x] 5. TeamComponent: collapsed gates → _canManageMembers (HasClaim MemberManage)
       (note: existing _canManage = team:manage was left untouched)
- [x] 6. Tests: deleted ImpliedScopesTests; fixed SimplifyRegistrationTests asserts
- [x] 7. Docs: scopes table trimmed; Umbrella section removed
- [x] 8. build.yml MAJOR_MINOR already 3.1 (inherited — #105/#106 merged to master; no change needed)
- [~] 9. Build + full test run (462/462 green); commit; present

## Notes
- Breaking vs 3.0.x → 3.1.0. Composes with async PR #106 (also 3.1.0); build.yml bump may trivially
  coincide (same value) if both merge — git resolves identical changes.
- All implied-scopes references are confined to the files above (verified) — clean revert.

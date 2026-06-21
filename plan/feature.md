# Feature: Remove granular member scopes (consolidate to member:manage)

## Goal
Remove the granular `member:invite` / `member:remove` / `member:role` scopes and use the single
`member:manage` scope for all member management. Member management becomes all-or-nothing.

## Design (confirmed with user)
- Remove the three granular constants + their registration; `member:manage` becomes a plain scope.
- **Remove the implied-scopes mechanism** too (it was unreleased, added in #102 only to make member:manage
  subsume the three — now pointless): revert `ScopeDefinition.Implies`, the `Register(implies:)` overload,
  and `ExpandImplied` in `GetEffectiveScopes`.
- Repoint internal usages to `member:manage`:
  - `ITeamManagementService`: 5 `[RequireScope(MemberInvite/Remove/Role)]` → `[RequireScope(MemberManage)]`.
  - `TeamComponent`: collapse `_canInvite/_canRemove/_canChangeRole` → single `_canManage`
    (`HasClaim(Scope, MemberManage)`).

## Breaking change → 3.1.0
- Removes scopes shipped in 3.0.x. Loses invite-only/remove-only/role-only granularity; consumers using
  a granular scope (or roles/overrides referencing one) must migrate to `member:manage`.
- `build.yml` `MAJOR_MINOR` 3.0 → 3.1 (self-contained; composes with the async PR which also targets 3.1.0).

## Scope
- TeamScopes, ScopeDefinition, ScopeRegistry, ThargaBlazorRegistration, ITeamManagementService,
  TeamComponent, tests (drop ImpliedScopesTests; fix SimplifyRegistrationTests), docs, build.yml.

## Acceptance criteria
- [ ] `member:invite/remove/role` constants + registration removed; `member:manage` registered (plain).
- [ ] Implied-scopes mechanism fully reverted (no `Implies`, no `ExpandImplied`, no `implies:` overload).
- [ ] `ITeamManagementService` member mutations require `member:manage`.
- [ ] `TeamComponent` gates member actions on `member:manage` (single `_canManage`).
- [ ] Tests updated (ImpliedScopesTests removed; SimplifyRegistrationTests fixed); full suite green.
- [ ] Docs updated (scopes table; Umbrella section removed).
- [ ] `MAJOR_MINOR` bumped to 3.1.
- [ ] Build + full test suite green on net9.0/net10.0.

## Done condition
PR `feature/remove-granular-member-scopes` → `master`; user confirms. Release 3.1.0.

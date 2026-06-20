# Feature: member:manage umbrella scope (implied scopes)

## Goal
Add a single `member:manage` umbrella scope that subsumes `member:invite` + `member:remove` +
`member:role`. A principal holding `member:manage` is authorized for all member-management operations;
the granular scopes stay for fine-grained cases. Default behavior unchanged for everyone who doesn't
grant `member:manage`.

GitHub issue: Tharga/Platform#102. Consumer: Eplicta FortDocs (gates team-member/group admin to team
admins — wants one scope instead of composing three; e.g. `[RequireScope("member:manage")]`).

## Design (confirmed with user)
General **implied-scopes** mechanism (reusable, not hardcoded to member:manage):
- `ScopeDefinition` gains `IReadOnlyList<string> Implies`.
- `ScopeRegistry.Register(name, level, description = null, implies = null)` — backward-compatible overload.
- `ScopeRegistry.GetEffectiveScopes` expands implied scopes **transitively** (cycle-safe, dedup). This is
  the single chokepoint, so the expansion reaches API keys, members (server + WASM), the team/api-key UI,
  and consumer `[RequireScope]` checks uniformly.
- Register `member:manage` at `AccessLevel.Administrator` implying the three granular scopes.

## Scope
- `TeamScopes.MemberManage = "member:manage"`.
- `ScopeDefinition.Implies` + `Register` overload + `GetEffectiveScopes` transitive expansion.
- Register `member:manage` in the default scope block (ThargaBlazorRegistration).
- Tests + docs.

## Out of scope
- The library does not itself gate member ops with `[RequireScope]` (these scopes are a consumer
  vocabulary) — no library enforcement change. member:manage appears in the scope picker automatically.

## Acceptance criteria
- [ ] `TeamScopes.MemberManage` exists; registered by default at Administrator implying the three.
- [ ] A non-admin granted `member:manage` (role/override) resolves effective scopes incl. invite/remove/role.
- [ ] A principal NOT granted member:manage is unaffected (no granular scopes added spuriously).
- [ ] Owner/Admin unaffected (already get all scopes); expansion is idempotent.
- [ ] Implied expansion is transitive and cycle-safe.
- [ ] Existing `Register(name, level, desc)` calls unchanged (backward compatible).
- [ ] Unit tests cover expansion, transitivity, cycle-safety, default registration.
- [ ] Docs updated (scopes table + umbrella/implied-scopes note).
- [ ] Build + full test suite green on net9.0/net10.0.

## Done condition
PR `feature/member-manage-scope` → `master`; user confirms after testing.

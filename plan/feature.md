# Feature: Show system vs team scopes in ScopeView

## Goal
Let a user see, in `ScopeView`, which scopes are **system** vs **team**. Team scopes keep the existing
interactive explorer; system scopes get a **separate table** listing only the system scopes the
**current signed-in user actually holds** (not the full catalog).

## Design (confirmed with user)
- Team scopes: existing grid unchanged (access-level/role selectors, grant resolution, grey-out).
- System scopes: separate table — the user's `Scope` claims ∩ registered system scopes
  (`ISystemScopeRegistry`). Columns: Scope, Description. Empty-state when the user has none.
- New `[Parameter] bool ShowSystemScopes` (default true). System section shown only when system scopes
  are registered. Headings ("Team scopes" / "System scopes") appear when both sections render.
- Pure helper `ScopeReference.UserSystemScopes(ISystemScopeRegistry, IEnumerable<string> userScopes)` for
  testability (no DI/rendering).

## Why this shape
System scopes are a flat set granted to system keys / via system roles — they don't fit the team
access-level/role grant model, so merging them into the team grid would be misleading. A separate
"your system scopes" table is accurate and focused. The user's system scopes reach their claims via
`TeamServerClaimsTransformation` (system-role → scope), so claims ∩ system registry is the source.

## Acceptance criteria
- [ ] `ScopeView` shows a separate System scopes table of the current user's held system scopes.
- [ ] Team grid behavior unchanged.
- [ ] `ShowSystemScopes` parameter (default true); system section hidden when no system scopes registered.
- [ ] Empty-state when the user holds no system scopes.
- [ ] `ScopeReference.UserSystemScopes` helper + unit tests; ScopeView param test.
- [ ] Build + full test suite green on net9.0/net10.0.

## Done condition
PR `feature/scopeview-system-scopes` → `master`; user confirms.

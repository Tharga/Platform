# Feature: Access-guard ordering and the `mcp:discover` scope model

Two defects reported by PlutusWave against the shipped 3.7.0, both found by inspection rather than by
running the app. Unrelated in code, grouped because each is small and both are consumer-facing corrections
to the same release.

## 1. `ApiKeyView` flashes "Access denied" on every load

`ApiKeyView.razor` checks `!_hasAccess` before `!_teamLoaded`. Both fields default to `false`, and
`_hasAccess` is only assigned part-way through `OnInitializedAsync` after two awaits. Blazor renders once
before those resolve, so the first frame always takes the denial branch — the `<Loading />` branch that
exists to cover it is unreachable.

A user with full rights sees *"Access denied. You need the API key management permission to view this
page."* flash before their keys appear.

**Fix:** evaluate `!_teamLoaded` first. No new UI — the loading state is already written.

**Beyond the reported fix**, extract the branch choice into a pure, tested resolver rather than leaving it
as markup ordering. The bug *is* the ordering, and ordering in markup is exactly what this project cannot
test today. A resolver makes the rule assertable and makes the same mistake fail a test next time.

**Also audit the sibling views for the same shape** — any component that initialises an access flag to
`false` inside an async lifecycle method and renders a denial before a "loaded" flag has this defect. As
the request puts it: a denial rendered from an unresolved state is worse than a blank one, because it tells
the user something untrue about their permissions and trains them to ignore a message that is sometimes
real.

## 2. `mcp:discover` is registered as a team scope but checked as a system scope

`AddTeam()` registers `McpScopes.Discover` into the **team** registry at `AccessLevel.Viewer`, so holders
receive it as a `TeamClaimTypes.Scope` claim. `McpScopeChecker.Has` reads **only**
`TeamClaimTypes.SystemScope`. The scope is therefore unsatisfiable through the route that grants it.

Currently latent: nothing in `Tharga.Team.Mcp` or `Tharga.MongoDB.Mcp` calls `IMcpScopeChecker`. It bites
the first consumer that follows the documented pattern and calls `scopeChecker.Require(McpScopes.Discover)`
in a tool — that tool then rejects every caller, including a team Owner.

**Decision (user, 2026-07-30): make it grantable every way.**

- **Checker accepts both provenances** — a system grant authorizes anywhere; a team grant authorizes when
  it is bound to the team the caller actually selected. Resolved through `TeamScopeGate`, never a bare
  `HasClaim`, so a scope granted on team A cannot satisfy a check made in team B. That unbound form is the
  hole plan 02's `Scope`/`SystemScope` split closed, and this fix must not reopen it.
- **Registered in both registries** — the existing team registration stays, so `AccessLevel.Viewer` keeps
  meaning what the XML doc says; a system registration is added so a system API key can hold it.

## Out of scope

- **Removing `IMcpScopeChecker`.** Plan 05 item 5 intends to remove or demote it as a second place that
  authorizes. This change keeps it working correctly until then; it does not entrench it further.
- **The `ConfigureSystemScopes` auto-registration note** attached to the second request — already
  documented in 3.7.1.

## Acceptance criteria

- [ ] `ApiKeyView` renders the loading state, not a denial, before initialization completes.
- [ ] The branch choice is a pure, unit-tested resolver rather than markup ordering.
- [ ] Sibling views are checked for the same shape; any found are fixed or explicitly cleared.
- [ ] A team member at `AccessLevel.Viewer` or above satisfies `mcp:discover` **for their selected team**.
- [ ] A holder of the scope for a different team does **not** satisfy it.
- [ ] A system-role or system-API-key holder satisfies it with no team selected.
- [ ] `McpScopes.Discover` XML documentation matches the shipped behaviour.
- [ ] Full test suite passes.

## Version

Both are fixes; `MAJOR_MINOR` stays `3.7` and CI increments the patch. **One behaviour change worth a
release note:** `mcp:discover` moves from unsatisfiable-by-team-grant to satisfiable, so a tool calling
`Require(McpScopes.Discover)` starts accepting callers it previously rejected. That is the point of the
fix, but a consumer who wrote a test asserting the rejection will see it change.

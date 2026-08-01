# Plan: Enforce `team:read` on team reads

## The rule this implements

**Every first-level call — user, REST, or MCP — goes through a scope-checked service. Those services may
call internal ones. Framework code is internal throughout, because no user triggers it.**

Layers, and where each stands:

| Layer | Rule | Today |
|---|---|---|
| Blazor views, REST controllers, MCP providers | first-level → scope-checked | ❌ inject `ITeamService` directly |
| `ITeamManagementService` | the scope-checked entry point | ⚠️ **has no read methods at all** |
| `ITeamService` | internal path, no checks | ✅ correct, but injectable by anything |
| Claims builder, revalidation | framework → internal | ✅ already correct |

## Steps

- [x] **Update NuGet packages** — only `SixLabors.ImageSharp 3.1.12 -> 4.0.0`, deliberately held (paid
      licence from 4.0). Nothing else outdated.
- [x] **Establish the defect and its blast radius.** `TeamScopes.Read` registered and never enforced on a
      read; invisible for Viewer-and-above, real wherever a principal's effective scopes lack it.
- [x] **Find what a naive gate would break.** Two claims-bootstrap paths and the invite flow — which is
      what showed the layering, not the gate, was the thing that was wrong.
- [x] **Settle the approach with the user.** Recorded in `feature.md`.

### Give first-level callers something correct to call

- [ ] **Scope-checked reads on `ITeamManagementService`**, carrying `[RequireScope(TeamScopes.Read)]`:
      team details, the roster, and a single member.
- [ ] **`GetInvitationAsync(inviteCode)`** — the invite entry point, authorized by the **code** rather than
      a scope, since an invitee is not yet a member. Replaces "read any team by key, then filter in memory".

### Move every first-level surface onto them

- [ ] **Blazor read views** — `TeamComponent`, `TeamsListView`, `UsersListView`, `TeamDialog`,
      `TeamInviteView`, `ScopeView`.
- [ ] **MCP `TeamResourceProvider`.** Today MCP's only automatic gate is the provider's *scope class*
      (`McpScope.System` → Developer role, `McpScope.Team` → membership); it never consults a scope, and
      `IMcpScopeChecker` is opt-in with nothing calling it. Routing providers through the gated service
      means an API key's scopes are checked exactly like a user's, by the same code.
- [ ] **`AccessPage` in the sample** — a host injecting `ITeamService` directly. It is the worked example
      consumers copy, so it has to follow the rule it teaches.

### Make the bypass hard

- [ ] **`[EditorBrowsable(EditorBrowsableState.Never)]` on `ITeamService`** plus XML docs saying it is the
      host's implementation contract and not for injection. Non-breaking; removes the discoverability that
      causes the mistake.
- [ ] **Architecture test** — no component, controller or MCP provider in this repo may inject
      `ITeamService`. Fails naming the offending type, so the next surface added cannot reintroduce it.
- [ ] **Runtime backstop** — a `Tharga.MongoDB` `ICollectionInterceptor` rejecting a team-collection
      operation when no scope check was recorded for the current call. `InterceptionPoint.Invocation` is
      documented for exactly this ("runs while the caller's ambient context is still in scope"). Must be
      cheap: read an ambient marker set by `ScopeProxy`, nothing more.

### Finish

- [ ] **Tests** — a caller without `team:read` refused at every first-level surface; a Viewer member
      unaffected; both framework paths still working; invite acceptance still working.
- [ ] **Verify the tests fail for the right reason** — remove a gate and confirm the failure names it.
- [ ] **Verify** — full suite plus an explicit sample compile (`-t:Compile`).
- [ ] **Bump `MAJOR_MINOR` 3.8 → 3.9** — behaviour-changing for callers lacking `team:read`.
- [ ] **Consumer docs** — an "inject this, not that" table in the Service README and the implementation
      guide, with the reason stated once.
- [ ] **Shared instructions** — a "Service layering" rule so future sessions apply this by default.
- [ ] **Update spec 06** — this resolves the phase-3 question that was blocking the matrix.

## Notes

**Scopes are what matter, not levels.** Access levels and roles are ways to *derive* scopes and overrides
add to them, so the check belongs on the scope. `AccessLevel.Custom` is simply the only level that can
currently lack `team:read`, since it is registered at Viewer and every other level inherits it implicitly.

**The analyzer is deliberately not here** — filed as its own feature. Everything above protects *this*
repo; a consumer injecting `ITeamService` still gets unchecked reads with no build-time warning. The
interceptor is the runtime half of that answer, the analyzer the build-time half.

**Why the invite path is not a leak.** Corrected in `feature.md`: the view renders only the team name, the
roster stays in server memory, and a bad code and a missing team look identical. It constrains the design
without being an exposure.

## Last session

Approach settled with the user, analyzer split out to the backlog, `ICollectionInterceptor` confirmed as
the runtime backstop mechanism. Starting implementation with the read methods.

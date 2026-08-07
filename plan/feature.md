# Feature: guard the internal-service boundary

## Goal

Make it impossible to quietly reopen the `team:read` hole, and correct the records that describe it as open.

## How this started, and what changed

Picked up as "close the `team:read` hole" — the backlog said the gated read path existed but **no surface had
moved onto it**, so `AccessLevel.Custom` still read team metadata, the full roster and API-key metadata, and
plan `07-move-surfaces-to-gated-reads.md` was the remaining half.

**Verifying that first showed the entry was stale.** The work it described has shipped:

| Backlog claim | Reality (2026-08-07) |
|---|---|
| Every read surface injects `ITeamService` | **No `.razor` component injects it at all.** `TeamComponent` uses `ITeamManagementService` / `ITeamDirectoryService` / `ITeamOversightService` / `ITeamLifecycleService`; `TeamInviteView` uses `ITeamManagementService`; MCP's `TeamResourceProvider` and `TeamUserResourceProvider` use the gated facets |
| No read path checks `team:read` | `ITeamManagementService` carries `[RequireScope(TeamScopes.Read)]` on six reads |
| Needs a way to separate framework reads from caller reads — design before coding | Already built: `ITeamService` has `[EditorBrowsable(Never)]` plus the prescribed host-contract docs, and `TeamAccess.ForTeam` / `.System` / `.Unchecked` is the ambient mechanism |

Plan 07 was never written and is not needed.

**What was genuinely missing is the guard.** `shared-instructions.md` requires it and it did not exist:
*"Guard it with an architecture test, not a convention. Assert that no component, controller or MCP provider
injects an internal service... A convention nobody can run is how the hole reopens."* The correct structure
was held in place by nothing but the current authors' care — and the toolkit has already paid for that once,
since `team:read` came to be registered, documented and granted while enforced by nothing.

## Scope

- **`InternalServiceInjectionTests`** in `Tharga.Team.Blazor.Tests` (components) and `Tharga.Team.Mcp.Tests`
  (MCP providers). Each assembly guards its own first-level surfaces.
- Internal services discovered by `[EditorBrowsable(Never)]`, **not** a hard-coded list, so marking a new
  contract internal enrols it automatically.
- Blazor dependencies read from **both** constructor parameters and `[Inject]` properties — `@inject`
  compiles to a property, so a constructor-only scan would miss how components actually take dependencies.
- Classify and document the two non-component surfaces that legitimately inject `ITeamService`.
- Correct the stale backlog entry and the two `Requests.md` items that were already implemented.

## Acceptance criteria

- [x] A component or MCP provider depending on an internal service fails the build's test run.
- [x] The guards prove they bite — fixtures for both `[Inject]` and constructor injection, because every real
      surface passes and nothing else would demonstrate the check works.
- [x] The guards fail if discovery breaks or the internal-service set empties, rather than passing vacuously.
- [x] A test asserts the gated facets are **not** marked internal — otherwise every correct component would
      be reported and the natural fix would be to weaken the guard.
- [x] `TeamStateService` and `AccessSimulationState` document why they inject the unchecked contract.
- [x] No production behaviour change; full suite passes with no new warnings.

## Done condition

The boundary is enforced by something that runs, and no record still describes the hole as open.

## Deliberately not done

- **Making the guards transitive.** They check direct dependencies, as the shared instructions specify. A
  component injecting `ITeamStateService` reaches `ITeamService` one hop away; that is why the two stragglers
  are documented instead, and why their docs say not to follow the pattern.
- **`HttpContextMcpContextAccessor`** resolves `ITeamService` through the service provider rather than by
  injection, so no reflective guard can see it. It is auth-adjacent (resolving the team for a call), like
  `TeamContextResolver`, and legitimately internal.
- **Plan 01 §3b and §6b** — the startup registration sweep and registering `TeamAccessInterceptor`. Both
  genuinely outstanding, both larger than this, and §6b previously took the sample site down.

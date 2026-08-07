# Plan: guard the internal-service boundary

## Steps

- [x] 1. NuGet check (mandatory, up front). Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held for its paid
      build-time licence. Standing exception, nothing to apply.

- [x] 2. Verify the premise before building. This is what turned the task around — the backlog described
      open work that had already shipped. Recorded in `feature.md`.

- [x] 3. Component guard — `Tharga.Team.Blazor.Tests/InternalServiceInjectionTests.cs`.
      Discovers every non-abstract `IComponent` in the Blazor assembly (27 found) and asserts none depends on
      a type marked `[EditorBrowsable(Never)]`. Reads constructor parameters **and** `[Inject]` properties.
      Two fixtures prove it catches each form. Self-checks: >20 components discovered, internal set non-empty
      and containing `ITeamService`, and the gated facets are *not* internal.

- [x] 4. MCP guard — `Tharga.Team.Mcp.Tests/InternalServiceInjectionTests.cs`.
      Same shape over `IMcpResourceProvider` / `IMcpToolProvider` implementers. Kept in its own assembly
      rather than reaching across, so each assembly guards its own surfaces.

- [x] 5. Classify the two non-component injectors, in XML docs on each.
      `TeamStateService` → **Filtered**: a first-level read naming no team, so there is nothing for
      `[RequireScope]` to check; it recomputes visibility per item. `AccessSimulationState` → gated above by
      `SimulationScopes.Simulate`, and it must re-resolve the *real* grant, which a gated read cannot do for a
      caller who has simulated their scopes away. Both say **do not follow this as a pattern**.

- [x] 6. Build + full suite. `--no-incremental`: **0 errors, 11 warnings** (unchanged baseline —
      an incremental build reports only 5 because unchanged projects are skipped, which is misleading).
      **1810 tests pass**, up 37.

- [x] 7. Correct the records.
      Backlog (`Toolkit/Team.md`): the `team:read` entry marked **DONE** with the evidence, the two documented
      stragglers, and a pointer to what genuinely remains (plan 01 §3b and §6b). Original analysis retained,
      since it is still accurate about *why* it was hard.
      `Requests.md`: two items marked **Done** that had been sitting Pending after being implemented — the
      `UserServiceBase` cache decorator, and the persistence-extension-point startup check (done for
      `UserServiceBase`; `CreateTeamMember` remains as 4.0 work). Each names the workaround PlutusWave can now
      delete.

- [ ] 8. Close-out: archive `feature.md` to the Plan directory `done/`, `git rm -r plan`, final commit, push,
      open the PR. **Only when the user confirms.**

## Notes / decisions

- **Discovery by attribute, not by a list.** `[EditorBrowsable(Never)]` is already how the Internal row of the
  service-classification table is marked, so a new internal contract is enrolled without anyone remembering.
- **Every guard carries a proof that it fails.** Every real surface passes today, so without a deliberate
  violation fixture these tests could be broken — scanning the wrong member kind, comparing types that never
  match — and stay green forever while reading as protection.
- **No production code changed** beyond documentation. The two XML doc additions are the only edits outside
  the test projects.

## Last session

All steps except close-out complete. Nothing pushed, no PR.

Carried forward: plan 01 §3b (startup registration sweep, never built) and §6b (`TeamAccessInterceptor`
code-complete but unregistered — needs the bootstrap paths to declare `TeamAccess.System(reason)`).
Also still open: GitHub #176 (email sender, design settled), #155, #142.

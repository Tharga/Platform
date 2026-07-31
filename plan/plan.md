# Plan: Resolving the caller outside an HTTP request and outside a circuit

Feature scope in `feature.md`. Tests run before each commit; `plan.md` is updated as each step lands.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      *Done — only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, deliberately held (4.0+ needs a paid Six Labors
      build-time licence). Nothing to apply.*

- [x] **2. Reproduce it as a failing test**
      *Done — reproduced exactly, with the real `ServerAuthenticationStateProvider` unseeded, throwing the
      identical message the debugger showed. No inference needed: `BlazorTeamPrincipalAccessor` is the call
      site.*
      A unit test over `BlazorTeamPrincipalAccessor` with a null `HttpContext` and a real
      `ServerAuthenticationStateProvider` that has never been seeded — the state a non-circuit scope is in.
      It must throw the reported `InvalidOperationException` before the fix.
      **If it does not reproduce**, stop and say so rather than fixing by inference: the exception proves
      the mechanism but not the call site, and the accessor is only the prime suspect. In that case ask for
      the stack trace, which the debugger already had.

- [x] **3. Teach the accessor the third case**
      *Done — the outside-DI-scope failure yields no principal; everything else propagates.*
      **Detection was not available.** An unseeded `ServerAuthenticationStateProvider` is
      indistinguishable from a seeded one until it is called, and the framework exposes no "am I in a
      circuit" signal — so the condition can only be recognised from the exception. Matched on a narrow
      message marker (`"outside of the DI scope"`) rather than the exception type, because a *derived*
      circuit provider failing for a real reason is the case a type check would wrongly swallow. Recorded
      as a named constant with the reasoning beside it, since message matching is brittle by nature.
      No `HttpContext` and no circuit → no principal, not an exception. Shape to decide once the
      reproduction is in hand:
      - Detect the non-circuit state rather than catching blindly, if the framework exposes it.
      - Otherwise catch **only** `InvalidOperationException` from that specific call, and rethrow anything
        else. A broad catch would hide a genuine in-circuit failure, which is criterion 5.

- [x] **4. Tests for all four worlds** — *Done: 6 tests. HTTP request → request principal; in-circuit →
      circuit principal; neither → no principal, no throw; a plain provider failing for a real reason still
      throws; **and a provider deriving from `ServerAuthenticationStateProvider` failing differently still
      throws** — the case that proves the fix is narrower than "swallow InvalidOperationException".*
      HTTP request → the request principal. In-circuit → the circuit principal. Neither → no principal,
      no throw. In-circuit but the provider throws for a real reason → still throws.
      That last one is the one worth writing carefully; it is what stops the fix becoming a silent
      swallow.

- [x] **5. Verify build + full suite, commit** — *Done: 1071 passed / 0 failed; build clean (the one warning is pre-existing in `.Service`).*

- [x] **6. Verify against the running sample**
      *Done, and it corrected the diagnosis.* The first fix did **not** resolve the reported failure: the
      stack trace from the running app showed `UserServiceBase.GetClaims` making the same assumption, on
      the path MCP actually takes. The reproduction had confirmed the *mechanism*; I took it as confirming
      the *call site*, which the plan explicitly said were different things. Both sites now route through
      one shared `CircuitPrincipal.GetUserOrNullAsync` in `Tharga.Team`, so a third instance does not have
      to be found from a stack trace.
      **Outcome recorded for 06 phase 3:** `resources/read` on `team://team` with a team API key no longer
      crashes — it now fails with `Team '…' not found for the caller`, which is the code working correctly.
      `GetTeamsAsync()` resolves the current *user*; an API key has no user, so it falls outside the
      membership model. **`resources/list` and `resources/read` therefore disagree** — list self-gates on
      the `TeamKey` claim and returns three resources, read denies. That is invariant I5 failing today,
      found before the matrix was written. Recorded on the 06 spec as a decision phase 3 must make, not an
      assertion it can assume.
      `resources/read` on `team://team` with a team API key must stop returning `-32603`. Whether it then
      returns data or a clean authorization refusal is **not** this feature's business — record which,
      because it is the first real data point for 06 phase 3's matrix.

- [x] **7. Documentation** — *Reviewed, none needed, stated rather than skipped.* The fix removes a crash
      and changes nothing a consumer configures or calls. `CircuitPrincipal` is public because two packages
      need it, not because consumers are expected to; the background-work guidance added in the
      audit-actor release already covers the case a consumer would hit. The MCP behaviour change belongs in
      06's notes, where it is recorded.
      Only if the fix changes something a consumer would configure or rely on. A bug fix that removes a
      crash usually does not. Review, then state the outcome either way rather than skipping silently.

- [~] **8. Push and hand over for testing**
      Do **not** open the PR — the close-out commit must be last.

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`; note the outcome on `planned/06-audit-access-verification.md` (phase 1 done, and
what step 6 observed); archive `feature.md`; `git rm -r plan`; commit
`fix: principal-accessor-outside-blazor complete`; push; open the PR.

## Notes

- **Branched off master with PR #167 still open.** The two are independent — that one touches the audit
  entry builder, this one the principal accessor — so no conflict is expected. If #167 merges first, merge
  master in before close-out.
- **Third instance of the same mistake.** *No `HttpContext`, therefore X.* X was "a user" in
  `AuditHelper.BuildEntry` (#163), "a Razor circuit" here. Worth a sweep for other call sites reasoning
  the same way — but as a separate finding, not scope creep into this fix.
- **Do not let step 6 turn into "make MCP return data".** The crash and the access rules are different
  jobs; conflating them would let a permissive result look like success.

## Last session

Branch created off master after PR #167 (audit-actor-provenance) was opened. Pulled out of
`planned/06-audit-access-verification.md` as phase 1, because it is a live defect blocking MCP reads
entirely, and because `Tharga/Mcp#18` — the other half of making MCP usable — is being fixed in parallel
in that repo.

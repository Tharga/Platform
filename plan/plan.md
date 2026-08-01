# Plan: Finish the API-key surface

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-01, before the branch was cut.** One update available and
      deliberately NOT applied: `SixLabors.ImageSharp` 3.1.12 → 4.0.0 requires a **paid Six Labors
      build-time licence**. Everything else current.

---

## 2. Prove PR #169 works — DONE 2026-08-01

- [x] `Microsoft.AspNetCore.TestHost` added to `Tharga.Team.Mcp.Tests` — the **first integration test in
      this solution**. Kept to that one project; no harness.
- [x] Minimal host with a fake `IApiKeyAdministrationService` (the handler takes the interface, so no
      Mongo), `AddThargaMcp(mcp => mcp.AddTeam())`, `RequireAuth` left at its default.
- [x] 4 tests, all asserting **POST**: anonymous refused, team key accepted, system key accepted, unknown
      key refused.
- [x] **Broken deliberately, twice. The first version was worthless and the check caught it.**

### The first attempt proved nothing, and that is the finding

The test host called `AddAuthentication(ApiKeyConstants.SchemeName)` — making the API-key scheme the
**default**. A bare `RequireAuthorization()` then resolves to it whether or not `AddTeam()` contributed
anything, so all four tests passed with the fix disabled. A green test that cannot fail is worse than no
test: it converts an unverified claim into a verified-looking one.

Fixed by making the default scheme an **interactive** one (cookies here, OIDC in a real host) and
registering the API-key scheme alongside it. That is the actual condition the fix addresses.

**Second attempt, fix disabled: 3 of 4 fail.** The test is meaningful.

### What the failure mode confirms

With the contribution removed, an agent presenting a valid API key gets **302 Found** — a redirect to the
login page. That is precisely the symptom [Tharga/Mcp#18](https://github.com/Tharga/Mcp/issues/18)
describes, reproduced here for the first time. It also settles a loose end: **a scheme mismatch produces
302 or 401, never 404**, which is what should have ruled the pairing hazard out as the cause of
PlutusWave's 404 on sight.

**Acceptance criteria 1 and 2 met. PR #169 is verified.**

## 3. A policy meaning "any valid API key" — DONE 2026-08-01

- [x] `ApiKeyConstants.AnyKeyPolicyName` (`"AnyApiKeyPolicy"`) — authenticated against the API-key scheme,
      **no assertion on `IsSystemKey` in either direction**. Registered beside the existing two.
- [x] XML docs on all three now state the relationship: `PolicyName` **rejects system keys**,
      `SystemPolicyName` **rejects team keys**, and they are **disjoint, not a hierarchy**.
- [x] 14 end-to-end tests over four live endpoints.

**The trap is asserted, not just described.** The suite maps an endpoint per policy plus a `/both`
endpoint requiring `PolicyName` **and** `SystemPolicyName`, then walks the full matrix. `/both` admits
**neither** key kind — ASP.NET Core combines required policies, and these two are mutually exclusive.
That is the behaviour that pushed a consumer into hand-writing a policy, and with it a whole code path
out of test coverage. A comment saying so would have been forgotten; a red test cannot be.

The interesting property is the shape of the table — exactly one column accepts both rows — which is why
it is a `[Theory]` matrix rather than four separate facts.

**Also worth noting for the docs:** `Tharga.Mcp`'s `RequireAuth` policy already behaves like
`AnyKeyPolicyName` (it asserts nothing about `IsSystemKey`), so an **MCP** endpoint never needed a named
policy for this. The gap was only ever on the controller side.

## 4. ~~The `Tharga.Mcp` pairing hazard~~ — DROPPED 2026-08-01, the premise was wrong

**There is no incompatibility.** `ThargaMcpOptions.AuthenticationSchemes` is documented as *"Empty means
the application's default scheme"*, so `Tharga.Mcp` 1.0.1 paired with a bridge that contributes nothing
behaves **exactly as 1.0.0 did**. The combination loses the *benefit* of the new feature; nothing breaks.
Confirmed against the shipped XML docs, and reproduced by step 2's test — removing the scheme
contribution yields the documented 302 fallback, not a fault.

**Also: there is no `Tharga.Mcp` 2.0.0.** The version range proposed in `feature.md` guarded against a
version that has never been published. The user asking *"can Tharga.Mcp not use 2.0.0? I do not
understand the problem"* is what prompted re-deriving this instead of repeating it.

**How the item survived:** it was written while the /mcp 404 was still believed to be a pairing problem.
When that diagnosis was corrected it was demoted from *cause* to *"still real on its own merits"* — but
never re-checked. A claim that outlives the collapse of its only evidence is a claim nobody verified.

### The real defect, filed against `Tharga.Mcp`

`Tharga.Mcp` **1.0.0 → 1.0.1** is a *patch* that moved **ModelContextProtocol 1.4.1 → 2.0.0** — a major
upgrade of the SDK it wraps, carrying a stateless-by-default HTTP transport and `MCP9005` deprecations.
A consumer running `dotnet outdated -u` takes a patch without reading anything, because that is what a
patch number means. Filed in `Requests.md` under `## Tharga.Mcp`. **Nothing to build in this repo.**

## 5. Documentation — DONE 2026-08-01

- [x] `Tharga.Team.Service/README.md` — a "Which policy to gate an endpoint with" table covering all
      three, with the disjointness stated and the requiring-both trap as a warning callout.
- [x] `docs/articles/implementation-guide.md` — the same table where API-key auth is described. The old
      text named only `ApiKeyPolicy`, which is how a reader ends up assuming it is the general one.
- [x] Both say **MCP endpoints need none of these**: `UseThargaMcp()` builds its own policy admitting
      both key kinds, so naming one there narrows the endpoint rather than securing it.

## 6. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings.
- [ ] `MAJOR_MINOR` stays at **`3.9`** — one additive constant does not move the line.
- [ ] Archive `plan/feature.md` to `$DOC_ROOT/.../done/api-key-surface.md`
- [ ] `git rm -r plan`, final commit `feat: api-key-surface complete`
- [ ] Push, open the PR against `master`.

---

## Last session

**2026-08-01 (setup).** Branch cut off `master` after 3.9.0 merged. Package check done — no change.
Plan written, **awaiting confirmation before any code changes.**

**The finding that shaped this plan:** there is no integration test anywhere in the solution. Item 1 is
therefore not "write a test" but "introduce the first end-to-end test in this repo", which is why it is
scoped tightly to one project rather than as a harness.

**Next:** confirm the plan, then step 2.

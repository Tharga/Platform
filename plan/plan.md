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

## 4. The `Tharga.Mcp` pairing hazard

Covers acceptance criteria 4. **Settle the question in `feature.md` first.**

- [ ] Decide: a version range in the `.csproj` (refuses at restore, but constrains deliberate upgrades)
      or a startup check comparing the loaded assembly version against what the bridge was built for.
- [ ] Implement the chosen one.
- [ ] Test it, if the shape allows — a restore-time constraint is not unit-testable, which is itself an
      argument for the startup check.

**Worth remembering while doing this:** the hazard did **not** cause the `/mcp` 404 it was blamed for.
It is being fixed on its own merits, so resist making it carry more weight than it has.

---

## 5. Documentation

- [ ] `Tharga.Team.Service/README.md` — the three policies together, and the disjointness note.
- [ ] `docs/articles/implementation-guide.md` — same, where API-key auth is described.
- [ ] Note in the MCP docs that `RequireAuth = true` admits both key kinds since 3.8.2, so a host does
      not need its own policy for that case.
- [ ] Separate `docs:` commit before close-out.

---

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

# Feature: Finish the API-key surface

**Branch:** `feature/api-key-surface` (off `master`)
**Started:** 2026-08-01
**Release:** patch (3.9.x) — one additive policy constant, otherwise tests and packaging. `MAJOR_MINOR`
stays at `3.9`.

## Goal

Three items from the 2026-08-01 PlutusWave triage, all on the same surface. **The first is the reason
this is a feature rather than a chore:** a capability shipped in 3.8.2 that nothing anywhere exercises.

## Scope

| # | Item | Source | Why |
|---|---|---|---|
| 1 | Verify PR #169 end to end | Internal, from the triage | Shipped unverified; the only consumer never executes the path |
| 2 | A policy meaning "any valid API key" | Internal, from PlutusWave's workaround | The two built-in policies are disjoint, so requiring both admits nothing |
| 3 | The `Tharga.Mcp` pairing hazard | PlutusWave | A floor a consumer can over-bump into an incompatible combination, silently |

### Item 1 is the valuable one, and it needs infrastructure this repo does not have

**There is no integration test anywhere in this solution** — no `TestServer`, no
`WebApplicationFactory`, no host spun up in a test. Every existing test is a unit test over a pure
function or a substituted service. That is why `AddTeamTests` asserts only that the scheme lands in
`ThargaMcpOptions.AuthenticationSchemes`: **it proves the wiring exists, not that it works.**

The gap it leaves is exact. `AddTeam()` contributing the scheme only matters if
`UseThargaMcp()` then builds a policy from that list and the API-key handler authenticates against it.
Nothing tests the second half, and the one consumer on 3.8.2+ sets `RequireAuth = false` and applies its
own policy, so it never runs that code either.

**Feasible without Mongo:** `ApiKeyAuthenticationHandler` takes `IApiKeyAdministrationService`, an
interface, so a fake key store is enough. The test needs `Microsoft.AspNetCore.TestHost` added to
`Tharga.Team.Mcp.Tests` — the first such dependency in the repo.

### Deliberately not in scope

- **A general integration-test harness.** Add exactly what item 1 needs. A harness built before there
  are two consumers of it is a guess about the second.
- **Changing which policy `UseThargaMcp` builds.** That lives in `Tharga.Mcp`, a different repo.

## Acceptance criteria

1. **An end-to-end test proves an API key authenticates through the `RequireAuth` path.** A host that
   registers `AddThargaMcp(mcp => mcp.AddTeam())` and leaves `RequireAuth` at its default:
   `POST /mcp` with a valid team API key is **not** 401; anonymous **is** 401.
2. **The same test would fail without PR #169** — verified by removing the scheme contribution locally
   and watching it go red. A test that passes either way proves nothing, which is the situation being
   corrected.
3. **A named policy admitting any valid API key**, team or system. Documented alongside the existing two
   with a statement that those two are **disjoint, not a hierarchy** — the naming implies otherwise.
4. **An incompatible `Tharga.Mcp` pairing is detected and reported**, rather than manifesting as an
   endpoint that misbehaves.
5. Full suite green; no new warnings.

## Open question to settle before item 3 is coded

**Can the pairing hazard be detected at all from this side?** A `[MinimumVersion, NextMajor)` range in the
`.csproj` would refuse the bad combination at restore, which is the cleanest answer but constrains
consumers who upgrade `Tharga.Mcp` deliberately. The alternative is a startup check comparing the loaded
`Tharga.Mcp` assembly version against what the bridge was built for. Decide which before building.

## Done condition

All five criteria met, docs updated where the policy story changes, `plan/` removed in the close-out
commit, PR open against `master`.

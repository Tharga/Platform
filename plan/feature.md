# Feature: Prove a team API key cannot reach another team

## Goal

Close **I2** from the audit-access spec — *a team API key never reaches another team, even when it names
that team explicitly. Knowing a team key is not authority* — with tests rather than a live probe.

## Why it has stayed open

I2 is the invariant the user cared most about, and it has been unprovable through two phases for a
mundane reason: both API keys on the running sample belong to the same team, so no probe could ever
demonstrate confinement. The plan each time was "mint a key on a second team", which makes I2 depend on
someone's sample data and proves nothing after the app restarts.

The test does not need a second team to exist anywhere. It needs two keys with different `TeamKey` values,
which is a fixture.

## What is actually unproven

`AuditAccessTests` already covers I1 and I3 at the rule level, and `AuditAccess.CanRead` is correct. But the
rule only holds if the **principal a real team key produces** carries team-bound provenance. If
`ApiKeyAuthenticationHandler` issued `SystemScope` for a team key, or took the team from the request rather
than the key record, the rule would still be right and the system would still be broken.

Reading the handler, the design is right by construction: `key.TeamKey == null` selects between a system
branch (`IsSystemKey` + `SystemScope`, no team claim) and a team branch (`TeamKey` + `Scope`). The team
comes from the key record. **Nothing asserts any of that**, so the gap is coverage, not behaviour.

## Scope

Tests going through the real `ApiKeyAuthenticationHandler` into the real `AuditAccess.CanRead`:

- Two keys on different teams — each reads only its own (the case that was untestable)
- Naming another team explicitly is refused — I2 proper
- A team key cannot read across all teams — I1, end-to-end rather than rule-level
- The mechanism: a team key's scopes carry `Scope`, never `SystemScope`
- Contrast: a system key carries system provenance and no team claim
- Access level still gates: a Viewer-level key gets no `audit:read` even for its own team

## Not in scope

- **The phase 3 matrix.** Still waiting on the design decision about whether team-data reads should be
  scope-gated at all. This closes one invariant that does not depend on that answer.
- **Consent (I4).** C6's cross-team rows need the system-key consent decision first.
- **Live probing.** Superseded — a test proves more than a probe and keeps proving it.

## Acceptance criteria

- [ ] I2 proven by a test that names another team explicitly and is still refused
- [ ] The test drives the real handler, not a hand-built principal — otherwise it asserts the fixture
- [ ] The provenance mechanism is asserted, so a regression names the cause and not just the symptom
- [ ] Full test suite passes; sample compiles
- [ ] Spec 06 updated: I2 moves from untestable to proven

## Done condition

I2 is proven without depending on sample data, and a regression in either the handler's provenance or the
access rule fails a test that says which one broke.

## Version

No `MAJOR_MINOR` bump. Tests and documentation only — no consumer has to act.

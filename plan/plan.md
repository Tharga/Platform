# Plan: Prove a team API key cannot reach another team

## Steps

- [x] **Update NuGet packages** — mandatory first step. `dotnet outdated` reports only
      `SixLabors.ImageSharp 3.1.12 -> 4.0.0`, deliberately held (4.0+ requires a paid Six Labors
      build-time license). Nothing applied; nothing else outdated.
- [x] **Establish what is actually unproven.** `AuditAccessTests` already covers I1 and I3 against the
      rule. The gap is the principal a real key produces — read `ApiKeyAuthenticationHandler` and confirm
      the team/system branches and that `TeamKey` comes from the key record rather than the request.
- [x] **Write `TeamKeyConfinementTests`** driving the real handler into the real `AuditAccess.CanRead`.
      10 tests: the two-team case, I2 stated directly, I1 end-to-end, the provenance mechanism, the
      system-key contrast, and access level gating within the team.
- [x] **Verify the tests fail for the right reason.** Changed the team branch to emit `SystemScope`: the
      mechanism test failed *alongside* the three confinement tests, so the cause is named rather than
      leaving four symptoms. Restored, confirmed by an empty diff. Worth recording what that break means —
      one word, `Scope` to `SystemScope`, would let any team key read every team's audit and the system log.
- [x] **Verify** — 1135 tests pass; sample compiles.
- [x] **Update spec 06** — I2 marked proven in the invariants table and the acceptance criteria, with a
      section on why the live-probe framing was itself the blocker.
- [x] **Docs review** — both surfaces checked. Nothing needed correcting, but a real gap turned up: the
      Service README documented system keys without ever stating the confinement guarantee. Added a "What a
      key can reach" section — the boundary is a security property consumers reason about, not an internal
      detail. Root README and `docs/articles/` make no claims about key scope, so neither needed a change.

## Notes

**Why this stopped waiting for sample data.** I2 was blocked three times on "mint a key on a second team".
That framing made the invariant depend on someone's running app, and a probe proves it only until the next
restart. Two keys with different `TeamKey` values is a fixture, and the assertion survives.

**The handler is right; the coverage was missing.** `key.TeamKey == null` selects between the system branch
(`IsSystemKey` + `SystemScope`, no team claim) and the team branch (`TeamKey` + `Scope`), and the team comes
from the key record — so a request cannot name its way into another team. That is exactly I2, and nothing
asserted it.

**Assert the mechanism, not only the outcome.** A test that checks "cannot read team B" passes just as well
if the key were broken in some unrelated way. Asserting that a team key's scopes carry `Scope` and never
`SystemScope` is what makes a future regression report its cause.

## Last session

Feature complete. I2 is proven — the invariant the user cared most about, open since the spec was written.

1135 tests pass, sample compiles, spec 06 and the Service README updated.

Remaining: close-out (remove `plan/`) and PR.

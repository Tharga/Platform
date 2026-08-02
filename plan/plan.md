# Plan: Discovery matches readability, and the surfaces are exercised

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid-licence reason. Everything else current.

## 2. Version

- [x] **`MAJOR_MINOR` stays `3.10`.**

---

## 3. Discovery matches readability

- [ ] The audit resource is listed on the same evidence the read uses — not `IsDeveloper`, and not merely
      "the logger is registered", which is a *registration* condition rather than a caller one.
- [ ] The other system resources keep the role check. This narrows one gate, not all of them.
- [ ] **Decide it in one place.** A list check and a read check that each restate the rule is the shape
      this whole area has been fixing; if listing has to ask a different question, that is a finding.
- [ ] Tests both ways: the scope-without-role caller sees it, the role-without-scope caller does not.

## 4. Exercise REST end to end

- [ ] A real request through the pipeline, with `TestServer` — the pattern `ApiKeyPolicyTests` uses.
- [ ] A **team key** reads its own team with no parameter, and cannot reach another by any route.
- [ ] A **system key** with no header reads system-wide; with the header reads a consenting team; with the
      header for a non-consenting team is refused.
- [ ] The claims path: a request reaching an endpoint that only declares `[RequireScope]`.

## 5. Exercise MCP end to end

- [ ] Through the real accessor and provider dispatch rather than by constructing a context by hand.
- [ ] The same expectations as §4, so a divergence shows up as a failing assertion rather than as a
      difference nobody compared.

## 6. The UI column

- [ ] bUnit: the audit control is **absent** for a caller who cannot read, not present and refused.
- [ ] The PR #126 lesson, and the reason `AuditLogView` resolves access before rendering.

## 7. The spec records the re-scope

- [ ] Rewrite phase 3 in `$DOC_ROOT/.../planned/06-audit-access-verification.md`: what was built, and why
      the remaining cells were cut — one enforcement point, one resolver, 45 tests already covering the
      model. **State the condition that would make them worth building again**, so this is a decision with
      a trigger rather than an abandonment.

## 8. Documentation

- [ ] Discovery matching readability, if it is consumer-visible on the MCP surface.
- [ ] Separate `docs:` commit before close-out.

## 9. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] Archive to `$DOC_ROOT/.../done/audit-discovery-and-surfaces.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #186 merged. Package check unchanged.

**The re-scope came from asking what the matrix would still prove.** It was specced for a world where
three surfaces each held their own rule; phase 3a and the team-context work removed that. Counting what
exists — 45 tests across six files — showed the access model already covered, and the cells left over
mostly re-deriving one gate.

**And asking the question found a live defect**, which is the better argument for the re-scope than the
counting: `ListResourcesAsync` still gates on `IsDeveloper` while `ReadResourceAsync` uses `audit:read`,
so the list and the read disagree in *both* directions. It was introduced when the read gate moved and the
list gate one method away was not looked at.

**Next:** §3.

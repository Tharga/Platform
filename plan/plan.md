# Plan: Audit consent for keys, and one audit gate

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid-licence reason. Everything else current.

## 2. Version

- [x] **`MAJOR_MINOR` stays `3.10`** — releases as `3.10.3`.

---

## 3. Consent in the audit gate

- [ ] Keep `AuditAccess.CanRead(principal, teamKey)` exactly as it is — the claims-only answer, which is
      correct and free whenever the caller holds a team or system scope.
- [ ] Add `CanReadAsync(principal, teamKey, resolver)`: the sync answer first, and **only if that says no**
      a consent lookup through `TeamGrantResolver`.
- [ ] The consented level must yield `audit:read` through the *scope registry*, not by comparing access
      levels. `audit:read` sits at `Administrator` today; hard-coding that here would be a second copy of
      a fact the registry owns and a host can change.
- [ ] Ordering is the whole efficiency story: claims first means a normal caller pays nothing.

## 4. One gate on all three surfaces

- [ ] MCP's audit resource moves from `IsDeveloper` onto the shared gate.
- [ ] **This changes who can read audit over MCP** — a Developer without `audit:read` loses it, a holder
      of `audit:read` without the role gains it. That is the point of I5, but it is a behaviour change and
      belongs in the release note, not only in the diff.
- [ ] Check whether the other MCP system resources have the same problem. If they do, say so and scope it
      — do not quietly widen this feature to cover them.

## 5. Tests — the consent rows, at every level

- [ ] **I4a** no consent → no access, key and user alike.
- [ ] **I4b** consent below `Administrator` → still refused. One test per level: None, Viewer, User.
- [ ] **I4c** consent at `Administrator` → allowed, and *only* the scopes that level carries.
- [ ] **I5** one set of expectations, run against UI, REST and MCP. A table the three share, not three
      tables that happen to agree today.
- [ ] **The efficiency claim**, asserted rather than assumed: a caller with a team scope triggers no
      consent lookup. Otherwise §3's ordering is a comment, not a behaviour.
- [ ] Mutation-check each guard.

## 6. Documentation

- [ ] The consent decision, and that it applies to keys — this is the answer to a question the spec left
      open, so it belongs in the docs rather than only in a plan file.
- [ ] The MCP gate change, called out as a behaviour change.
- [ ] Separate `docs:` commit before close-out.

## 7. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] **Rewrite the spec's "Open decision" section as decided**, in
      `$DOC_ROOT/.../planned/06-audit-access-verification.md`, so the remaining matrix feature starts with
      nothing open.
- [ ] Archive to `$DOC_ROOT/.../done/audit-consent-for-keys.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #184 merged. Package check unchanged.

**Two things the survey changed.** First, I reported that MCP already granted consent-derived audit
access and that REST/UI needed to catch up. **That was wrong** — MCP's audit resource gates on
`IsDeveloper` and never consulted team scopes at all, so my stop-7 work did not touch it. Checking before
asserting turned a propagation job into a different, better-founded one.

Second, that mistake surfaced a real defect: the three surfaces use **two different rules**, and
`AuditAccess` was extracted specifically to stop that. It is an I5 violation sitting on master, unrelated
to the consent question and true regardless of how it was answered.

**Next:** confirm the plan, then §3.

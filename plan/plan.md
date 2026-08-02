# Plan: Select a team on an MCP call

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid-licence reason. Everything else current.

## 2. Version

- [x] **`MAJOR_MINOR` back to `3.10`** (user, 2026-08-02). Committed first, so nothing in this branch can
      publish under 3.11 by accident.
- [ ] Leave the pending 3.11.0 `release` job unapproved. `security` and `build` already passed; only
      `release` is gated, so not approving it is enough — nothing was published.

---

## 3. Confirm the two open decisions — **before any code**

- [ ] **Header name** (`X-Team-Key` proposed). Consumer-visible the moment it ships.
- [ ] **Unreachable team: refuse or see nothing?** Proposed **refuse**, so it reads differently from an
      empty team.

---

## 4. Carry the selection

- [ ] Read the header in `HttpContextMcpContextAccessor`, where `IMcpContext` is already assembled from
      `HttpContext`. One place, so no provider signature changes and no provider can forget it.
- [ ] `TeamMcpContext.TeamId` becomes *the selected team* rather than the claim, falling back to the
      claim when nothing is selected.
- [ ] The `McpScope` derivation must not regress: a system caller stays `System` whether or not they
      select, and a selecting user must not be promoted past `Team`.

## 5. Resolve what the selection grants — the load-bearing half

- [ ] Membership first: if the caller is a member of the named team, their membership scopes.
- [ ] Consent second: if not a member, the team's consented level for a global role the caller holds.
      This is the case that is unimplementable today and the reason the feature exists.
- [ ] **Intersect with what the caller already holds.** Selection narrows, never widens.
- [ ] Reuse the existing rule rather than restating it — `TeamMembershipClaimsBuilder` computes exactly
      this for Blazor, and a second copy is how the two enforcement paths would drift. If it cannot be
      shared as-is, extract the shared part rather than duplicating.

## 6. Refuse what cannot be reached

- [ ] Unknown team key, and a team the caller has neither membership nor consent for: refused,
      distinguishably from an empty team.
- [ ] No selection at all: unchanged behaviour. The feature is additive.

## 7. Tests

- [ ] The four grant cases: member, system key, consented non-member, neither.
- [ ] **The narrowing property**, attacked directly: a caller naming a team must not gain a scope they
      did not already hold. Worth several tests from different directions — it is the security claim.
- [ ] Refusal distinguishable from empty.
- [ ] A no-selection call is byte-for-byte what it is today.
- [ ] Mutation-check each guard by removing it.

## 8. Documentation

- [ ] `README.md` — the MCP section gains team selection.
- [ ] `docs/` — the header, what it grants, and the narrowing rule.
- [ ] `Tharga.Team.Mcp/README.md` — the package's own doc.
- [ ] Separate `docs:` commit before close-out.

## 9. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] Archive to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/mcp-team-selection.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #182 merged. `MAJOR_MINOR` reverted to `3.10` as
the first commit. Package check unchanged.

**Surveyed first, and it shaped the plan:** the MCP surface derives everything from the `TeamKey` claim —
`TeamMcpContext` reads it in its constructor, `McpScopeChecker.Has` resolves team scopes against it, and
`HttpContextMcpContextAccessor` derives `McpScope` from it. So the selection has to land in the accessor,
which is the one place all three already agree on.

**Next:** confirm the two decisions in §3, then §4.

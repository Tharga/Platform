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

- [x] **Header name** `X-Team-Key` (user, 2026-08-02). Consumer-visible the moment it ships.
- [x] **Unreachable team: refuse** (user, 2026-08-02), so it reads differently from an
      empty team.

---

## 4. Carry the selection

- [x] Read the header in `HttpContextMcpContextAccessor`, where `IMcpContext` is already assembled from
      `HttpContext`. One place, so no provider signature changes and no provider can forget it.
- [x] `TeamMcpContext.TeamId` becomes *the selected team* rather than the claim, falling back to the
      claim when nothing is selected.
- [x] The `McpScope` derivation does not regress: a system caller stays `System` whether or not they
      select, and a selecting user must not be promoted past `Team`.

## 5. Resolve what the selection grants — the load-bearing half

- [x] Membership first: if the caller is a member of the named team, their membership scopes.
- [x] Consent second: if not a member, the team's consented level for a global role the caller holds.
      This is the case that is unimplementable today and the reason the feature exists.
- [x] **Narrowing, sharpened: the selected team *replaces* the anchored one.** Selection narrows, never widens.
- [x] Reused via `TeamGrantResolver`, extracted to `Tharga.Team.Service` — `TeamMembershipClaimsBuilder` computes exactly
      this for Blazor, and a second copy is how the two enforcement paths would drift. If it cannot be
      shared as-is, extract the shared part rather than duplicating.

## 6. Refuse what cannot be reached

- [x] Unknown team key, and a team the caller has neither membership nor consent for: refused,
      distinguishably from an empty team.
- [x] No selection at all: unchanged behaviour. The feature is additive.

## 7. Tests

- [x] The four grant cases: member, system key, consented non-member, neither.
- [x] **The narrowing property**, attacked directly: a caller naming a team must not gain a scope they
      did not already hold. Worth several tests from different directions — it is the security claim.
- [x] Refusal distinguishable from empty.
- [x] A no-selection call is byte-for-byte what it is today.
- [x] Mutation-checked: removing the narrowing guard reds 1 test, removing the refusal reds 3.

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

**2026-08-02 (§3-§7).** Both decisions confirmed; selection implemented and tested. **1416 green**, 8 warnings.

**The narrowing rule was vaguer in `feature.md` than it needed to be.** "Intersected with what the caller
holds" is really *replace, do not accumulate*: team scopes are always recomputed for the named team, and
the principal's own Scope claims — which describe a **different** team — are never consulted. System
grants are untouched, being team-independent. `Selecting_DoesNotCarryTheAnchoredTeamsScopesAcross` is
the test that says so.

**Extracted rather than duplicated.** `TeamGrantResolver` now lives in `Tharga.Team.Service`, which both
Blazor and MCP reference; the Blazor claims builder delegates to it. Behaviour-neutral, verified by the
full suite before any MCP code was written.

**One wrinkle worth knowing:** the default consent level is configured in two places — `ConsentOptions`
(Blazor) and `McpTeamOptions.ConsentAccessLevel` — because the former lives above this package. A host
changing one must change the other, and the XML doc says so.

**Next:** §8 documentation, then close-out.

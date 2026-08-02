# Plan: The team comes from the credential, not from a parameter

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid-licence reason. Everything else current.

## 2. Version

- [x] **`MAJOR_MINOR` stays `3.10`.**

---

## 3. One place that resolves team context

- [ ] A single component that answers *"which team is this request about, and may this caller act there?"*
      from the credential plus the header. Both surfaces call it; neither reimplements it.
- [ ] **Issue `TeamKey` and `Scope` claims** for the resolved team, so every existing `[RequireScope]`
      check works untouched. This is the whole reason to do it in one place — a host's controllers are
      covered without knowing the mechanism exists.
- [ ] Header name shared with MCP's existing `X-Team-Key`. One name, or the two surfaces diverge again in
      the smallest possible way.

## 4. The three cases

- [ ] **Team key:** team from its `TeamKey` claim. Already present — nothing to resolve.
- [ ] **System key, no header:** no team context; system-scoped operations only.
- [ ] **System key, header:** resolve through `TeamGrantResolver`, the copy of that rule the claims builder
      and MCP already share. Refused when the team has not consented.
- [ ] **Team key with a header: refuse.** A contradiction, not a preference — ignoring it would leave the
      caller believing they asked for something they did not get. Same reasoning as refusing an
      unreachable team on MCP rather than answering empty.

## 5. The audit endpoint stops naming a team

- [ ] Remove `teamKey` from the query string.
- [ ] Team key → its own team. System key → oversight, with filters. System key + header → that team.
- [ ] **The team filter survives, as a filter.** For the system-audit case it narrows already-authorized
      data, which is what the component does. With a header present the team is fixed and a conflicting
      filter must not widen or narrow past it.
- [ ] Delete the try-team-then-fall-back-to-oversight branch. The credential decides which path applies,
      so there is nothing left to guess — that branch existed only because the endpoint could not tell.

## 6. MCP uses the same resolution

- [ ] `HttpContextMcpContextAccessor` moves onto the shared component rather than resolving the header
      itself, so the two surfaces cannot drift.
- [ ] Behaviour should not change for MCP; if it does, that is a finding and the difference gets named.

## 7. Tests

- [ ] Each of the four cases in §4, on **both** surfaces from one shared table.
- [ ] A team key cannot reach another team **by any route** — no parameter, and the header refused.
- [ ] A system key with no consent is refused; with consent, gets exactly the consented level.
- [ ] The team filter narrows without authorizing: a filter naming a team the caller cannot reach returns
      nothing rather than that team's data.
- [ ] **The claims path**: a host controller with `[RequireScope]` and no knowledge of the header works
      for a system key naming a team. That is criterion 6 and the reason for §3.
- [ ] Mutation-check each guard.

## 8. Documentation

- [ ] The endpoint's shape changed — the guide documents the old query string, so it is wrong, not merely
      incomplete.
- [ ] The rule itself: an external caller never names a team.
- [ ] The header, on both surfaces, with the consent requirement.
- [ ] Separate `docs:` commit before close-out.

## 9. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] Note the breaking query-string change for the consumer follow-ups.
- [ ] Archive to `$DOC_ROOT/.../done/rest-team-context.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #185 merged. Package check unchanged.

**Scope settled before planning:** the rule applies to the **REST and MCP surfaces**, not the service
interfaces. 65 methods take `string teamKey`, `ScopeProxy` resolves its authorization target from those
arguments, and callers such as a cross-team oversight holder legitimately name a team they hold no
credential for. The ambiguity being removed is specifically *credential and parameter disagreeing*, which
only exists at the surface.

**One decision left open**, and building proceeds either way: whether "the team consented" means consent
configured at all, or the key matching a consented role. Taking the second as the conservative default
because it is what `TeamGrantResolver` already does — a key holds no roles, so it is refused. Loosening it
later is one change in one place.

**Next:** §3.

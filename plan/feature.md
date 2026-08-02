# Feature: Discovery matches readability, and the surfaces are exercised

**Branch:** `feature/audit-discovery-and-surfaces` (off `master`)
**Started:** 2026-08-02
**Release:** **`MAJOR_MINOR` stays `3.10`.**

Stop 8, phase 3 — **re-scoped**. Phases 1, 2 and 3a are done.

## Why the specced matrix is not being built

`06-audit-access-verification.md` sizes phase 3 at **108 cells plus 60 consent cells**. It was designed
when *"audit data is reachable from three surfaces by six kinds of caller — and nothing asserts the three
agree."* That premise no longer holds:

- **One enforcement point.** `IAuditReadService` / `IAuditOversightService` carry the scope attribute; no
  surface authorizes anything (phase 3a).
- **One team resolver.** REST and MCP resolve which team a call is about through the same code.
- **45 tests already cover the access model** — C1–C6, every consent level, I1 structural, I2 and I3
  proven, I5 by construction.

So most of the matrix would re-assert one gate through three doors. **The cells stopped earning their
keep the moment the thing they measure stopped being able to vary.** Recorded in the spec rather than
silently dropped.

## The live defect this starts from

`TeamSystemResourceProvider.ListResourcesAsync` still gates on `IMcpContext.IsDeveloper`, while
`ReadResourceAsync` routes audit through `audit:read`. The two disagree in both directions:

| Caller | Sees "Audit Log" listed | Can read it |
|---|---|---|
| Developer role, no `audit:read` | **yes** | no |
| System `audit:read`, no Developer role | **no** | **yes** |

The first advertises a resource the caller cannot read; the second hides one they can. **Introduced when
the read gate moved onto the service and the list gate beside it was not looked at** — the same shape of
oversight the audit feature was about, one method away from where it was fixed.

The spec names this class explicitly: *"a resource a caller cannot read must be absent from
`resources/list`, not merely fail on read. A discovery leak is its own class of bug."*

## Scope

1. **Discovery matches readability.** A resource is listed if and only if the caller could read it, decided
   by the same check the read uses.
2. **Exercise the surfaces end to end.** Everything today is unit-level: nothing issues a real HTTP request
   or MCP call through the pipeline. This session's bugs — a sample that would not start, a guard that
   found no files on Linux, three guards that passed while examining nothing — were all found by *running*
   something, never by reading it.
3. **The UI column.** The audit control must be **hidden** for a caller who cannot read, not shown and then
   refused. That is the PR #126 lesson, and the spec asks for it as bUnit coverage.

## Not in scope

The remaining matrix cells. If a future change reintroduces per-surface rules, they become worth building
again — and the spec will say why they were cut.

## Acceptance criteria

1. A caller who cannot read a resource does not see it listed, on the same evidence the read uses.
2. A caller who *can* read it does see it, including one holding the scope without the role.
3. An end-to-end REST request proves the credential-derived team context: a team key reads its own team, a
   system key with the header reads a consenting team, and neither can reach anywhere else.
4. An end-to-end MCP call proves the same, through the real accessor.
5. The audit control is hidden, not shown-then-refused.
6. The spec records why the matrix was cut, so the decision is not re-litigated from memory.
7. Full suite green; no new warnings (baseline 8).

## Done condition

All seven met, docs updated, `MAJOR_MINOR` still `3.10`, `plan/` removed in the close-out commit, PR open
against `master`.

# Feature: Audit consent for keys, and one audit gate

**Branch:** `feature/audit-consent-for-keys` (off `master`)
**Started:** 2026-08-02
**Release:** **`MAJOR_MINOR` stays `3.10`** — releases as `3.10.3`.

Stop 8, phase 3, part A. The spec is
`$DOC_ROOT/Tharga/plans/Toolkit/Platform/planned/06-audit-access-verification.md`; phases 1 and 2 are
done and invariant **I2** is already proven. This settles the decision that blocked phase 3, so the full
matrix can be built afterwards with nothing open in it.

## The decision that was blocking C6 — settled 2026-08-02 (user)

> *Does a system API key inherit a team's consented access level, and if so at what level?*

**Yes, at exactly the consented level** — the same rule as a user holding that role, and no higher. A
team that consents `Administrator` to `Support` admits a `Support` key at `Administrator`, which carries
`audit:read`; a team consenting `Viewer` admits nothing, because `audit:read` sits at `Administrator`.

The argument against was real and was weighed: consent is a team owner's decision about **roles**, made
thinking about colleagues rather than machines, so admitting keys widens what that decision meant. It
was rejected in favour of one rule for one grant — a role means the same thing whoever holds it, and a
second rule for keys is a second thing to get wrong.

## A second finding, and it is already on master

**The three surfaces do not share an audit gate, which is invariant I5's whole subject.**

| Surface | Gate today |
|---|---|
| UI | `AuditAccess.CanRead` |
| REST | `AuditAccess.CanRead` — the same function |
| MCP | **`IMcpContext.IsDeveloper`** — a different rule entirely |

`AuditAccess` exists precisely so *"every surface asks the same question of the same code"*, and its own
remarks say the rule restated per surface is the rule that drifts. MCP restates it. A caller holding
system `audit:read` but not the Developer role is refused on MCP and admitted on REST; the reverse is
also true. Neither is what either surface intends.

**This is not caused by the consent decision** — it predates it, and would still be wrong if the answer
above had gone the other way.

## Scope — revised 2026-08-02 (user)

> *"Accessing the API with REST and using MCP should render the same access with the same API key. It
> should be checked the same way — in the registered services, with attributes."*

That is the toolkit's own pattern, and **audit is the one thing that never joined it.** Everything else
is enforced by `[RequireScope]` on a registered service through `ScopeProxy`; audit is a static
`AuditAccess` that each surface has to remember to call, plus a different rule again on MCP. A shared
static gate — the first version of this plan — would still have been surface-level enforcement: three
call sites, each able to forget.

1. **Audit becomes a registered, attribute-gated service**, in the shape the codebase already uses for
   the team/system split:
   - `IAuditReadService`, registered with `AddTeamService` — names a team, `[RequireScope(AuditScopes.Read)]`
     checked against *that* team.
   - `IAuditOversightService`, registered with `AddSystemService` — no team, so the system grant is
     required. This is invariant **I1** expressed by the registration rather than by a check.
2. **Every surface calls the service and gates nothing itself.** REST controller, Blazor view and MCP
   resource all lose their own authorization. Divergence stops being possible rather than being tested for.
3. **`IsDeveloper` stops being an authorization input.**
4. **Consent for key callers**, per the decision above, resolved into claims rather than checked at a
   surface.
5. **The consent rows of the matrix**, at every consent level: I4a, I4b, I4c for C4 and C6.

## Why this is the right shape, and what it costs

`ApiKeyAuthenticationHandler` already emits `Scope` claims for a team key and `SystemScope` for a system
key, and `ScopeProxy` already resolves the target team from the method arguments. **So a team API key
already behaves identically on REST and MCP** for every attribute-gated service — audit is failing to use
a mechanism that works.

The gap is consent: a key gets no consent evaluation, because consent needs a *target team* and
authentication does not know one. The request does — MCP already names it in `X-Team-Key`. Accepting the
same header on REST and resolving the consented scopes into claims makes the two surfaces identical by
construction, and makes `[RequireScope]` the only thing anyone has to get right.

## The name

`IMcpContext.IsDeveloper` reads as a fact about a person; it is really *"holds the role named by
`McpTeamOptions.DeveloperRole`"*, which a host can set to anything. It lives in the **`Tharga.Mcp`
package**, so it cannot be renamed from here — filed as a cross-project request instead. What this
feature can do, and does, is stop treating it as authorization.

## Not in scope

**The full 108-cell matrix.** Separate feature, once this leaves nothing open in it.

## Acceptance criteria

1. A system key with a consented role reads that team's audit, at the consented level and no higher.
2. A consent level below `Administrator` grants no audit access — proven at **each** level, not just
   consent/no-consent.
3. No consent, no access — for a key and for a user alike.
4. UI, REST and MCP give the **same answer for the same caller** (I5) — because they share an enforcement
   point, not because three of them were tested into agreement.
5. No surface performs its own audit authorization; removing a surface's own check changes no outcome.
6. The same API key gets the same answer on REST and MCP, including when it names a team.
7. Full suite green; no new warnings (baseline 8).

## Done condition

All seven met, docs on both surfaces, the spec's open-decision section rewritten as decided, `MAJOR_MINOR`
still `3.10`, `plan/` removed in the close-out commit, PR open against `master`.

# Feature: Enforce `team:read` on team reads

## Goal

Make `TeamScopes.Read` mean what it says. It is registered at `AccessLevel.Viewer` as *"View team details
and members."*, granted, and documented — and no read path checks it.

## The defect

Two service interfaces, only one enforced:

| | Enforcement | Used for |
|---|---|---|
| `ITeamManagementService` | `[RequireScope]` via `ScopeProxy` | mutations |
| `ITeamService` | `AuthorizationTeamServiceDecorator` — *"Reads & self-service — pass through"* | **all reads** |

Both read surfaces — MCP's `TeamResourceProvider` and the Blazor team views — inject `ITeamService`. The
only two `[RequireScope(TeamScopes.Read)]` declarations in the codebase sit on `SetMemberLastSeenAsync` and
`SetInvitationResponseAsync`, which are *writes* that require read-level scope.

**Invisible for ordinary members**: `team:read` sits at Viewer, so every level from Viewer up holds it and
enforcing it changes nothing for them. **The hole is `AccessLevel.Custom`**, documented as *"least-privilege
machine keys that should carry only their explicit grants"*. `GetScopesForAccessLevel` returns empty for
`Custom`, so such a caller deliberately does not hold `team:read` — yet today it reads team metadata, the
full member roster with access levels and states, and API-key metadata.

## Why the obvious fix fails

Three read paths cannot require a caller scope. They are what makes "add a gate to the decorator" wrong,
and finding them before coding is the point of this section:

1. **`TeamMembershipClaimsBuilder`** calls `GetTeamMemberAsync` *while building the principal*. Requiring a
   scope there is circular — the scope comes from the claims being constructed.
2. **`TeamClaimsAuthenticationStateProvider`** does the same on revalidation.
3. **`TeamInviteView`** calls `GetTeamAsync<TMember>(teamKey)` for an invitee who is not yet a member and
   holds no scopes for that team.

A blanket gate on the decorator breaks sign-in and invitations. That is the signal: these are *internal*
reads sharing methods with *caller* reads, so the layering is what is wrong, not the missing check. The
`// Reads & self-service — pass through` comment is describing a correct decision at the wrong level.

Also relevant: `ITeam` carries no `Members` — only `ITeam<TMember>` does. So non-generic `GetTeamsAsync()`
is genuinely self-service metadata, while the generic overloads carry the roster.

## Approach — settled

The user's rule: **first-level methods (user / API / MCP) always go via scope checks; inside those
services internal paths are fine; framework code is internal throughout, since no user triggers it.**

That reframes the fix. The problem is not that the decorator lacks gates — it is that the first-level
surfaces bypass the scope-checked interface entirely, because that interface offers them nothing:

| Layer | Rule | Today |
|---|---|---|
| Blazor views, MCP providers | first-level → scope-checked | ❌ inject `ITeamService` directly |
| `ITeamManagementService` | the scope-checked entry point | ⚠️ **has no read methods at all** |
| `ITeamService` | internal path, no checks | ✅ already correct |
| Claims builder, revalidation | framework → internal | ✅ already correct |

So: add scope-checked reads to `ITeamManagementService`, move the surfaces onto them, and leave
`ITeamService` as the internal path it already is. The decorator needs no new gates, and the framework half
is already right — the circularity that looked like the hard part disappears, because the claims builder
is *supposed* to use the internal path.

This also dissolves the per-team scope question. `GetTeamsAsync<TMember>()` stays ungated as an internal
method; the entry point that calls it carries the check.

Rejected alternatives, and why:

- **Gating the decorator's reads directly.** What this feature started as. It breaks sign-in and
  invitations, because the framework and the invitee both read through the same methods a caller does —
  the layering, not the gate, was the thing that was wrong.
- **Ambient suppression flag** (an `AsyncLocal` "bootstrap mode"). Ambient state is the wrong shape for a
  security gate: invisible at the call site, easy to widen by accident, hard to prove is off.
- **Each surface checks the scope itself.** Exactly the drift the shared `AuditAccess` rule exists to
  prevent, and it contradicts architecture-v4's single enforcement point.

**The invite read is a functionality constraint, not a leak.** An earlier draft of this file claimed anyone
could read a team's roster by crafting an invite blob. That was wrong and is corrected here: the view
renders only `_team.Name`, the roster stays in server memory (Blazor Server, never serialized to the
client), and a bad code and a nonexistent team both simply hide the panel — so there is no oracle either.
The read is unauthorized at the service layer and exposes nothing. It matters only because gating
`GetTeamAsync<TMember>` would break a legitimate invitee, who is not yet a member and holds no scopes.

## Scope

- Scope-checked read methods on `ITeamManagementService`, carrying `[RequireScope(TeamScopes.Read)]`
- Move the Blazor read views and MCP's `TeamResourceProvider` onto them
- Leave `ITeamService` untouched as the internal path, and the claims builder and revalidation provider on
  it — they are framework, and already correct
- The invite flow keeps reading internally; its entry point authorizes on the invite code
- Tests per entry point, plus tests proving the framework paths still work

## Acceptance criteria

- [ ] A caller without `team:read` is refused team details, the roster and member lookups at every
      first-level surface
- [ ] A Viewer-level member is unaffected — the no-op case must stay a no-op
- [ ] No first-level surface injects `ITeamService` any more; a test asserts that, so the next view added
      cannot quietly reintroduce the bypass
- [ ] Sign-in works: claims construction still reads internally
- [ ] Invitation acceptance works
- [ ] Full suite passes; sample compiles

## Version

**Behaviour-changing — needs a `MAJOR_MINOR` bump.** A `Custom`-level caller that reads team data today
starts being refused. That is the fix, but a consumer relying on it must act, which is exactly the
condition for a bump.

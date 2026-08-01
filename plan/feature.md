# Feature: Move every first-level surface onto the gated read path

**Branch:** `feature/move-surfaces-to-gated-reads` (off `master`)
**Started:** 2026-08-01
**Release:** **`MAJOR_MINOR` 3.9 → 3.10.** This is where behaviour changes: a caller lacking `team:read`
that reads today starts being refused.

> **PR 2 of 2.** PR 1 (`gated-team-reads`, merged) made the path actually enforce `team:read` and added
> the interfaces a surface needs. This one moves the surfaces onto it and closes the hole.

## Goal

`team:read` is registered, documented, granted — and reachable around, because every read surface injects
`ITeamService`, the internal contract that is deliberately unchecked. PR 1 built the destination. Until
the surfaces move, the hole is open.

Invisible for ordinary members: `team:read` sits at `AccessLevel.Viewer`, so every level above inherits
it. It bites `AccessLevel.Custom` — *"least-privilege machine keys that should carry only their explicit
grants"* — which today reads team metadata, the full roster with access levels and states, and API-key
metadata.

## Scope — 11 surfaces move, 1 stays

| Surface | Calls | Destination |
|---|---|---|
| `TeamDialog` | `RenameTeamAsync` | `ITeamManagementService` |
| `InviteUserDialog` | `AddMemberAsync` | `ITeamManagementService` |
| `AuditLogView.razor.cs` | `GetTeamsAsync` | `ITeamDirectoryService` |
| `UsersListView` | `GetTeamsAsync`, `GetAllTeamsAsync` | Directory + Oversight |
| `TeamsListView` | + `DeleteTeamAsync`, `AssignOwnerAsync` | + Management |
| `TeamComponent` | + `GetTeamAsync`, `SetTeamConsentAsync` | all three |
| `TeamSelector` | + `SelectTeamEvent` | + the `TeamStateService` bridge |
| `TeamInviteView` | `GetTeamAsync`, `SetInvitationResponseAsync` | `ITeamInvitationService` + Management |
| `TenantRoleManager` | `GetTeamCustomRolesAsync` | **needs a gated member first** |
| `AccessPage` (sample) | `GetAllTeamsAsync` | `ITeamOversightService` |
| MCP `TeamResourceProvider`, `TeamUserResourceProvider` | four reads | Management + Directory + Oversight |

**`TeamStateService` stays on `ITeamService`.** Internal framework code, which the rule permits, and it
is what bridges `SelectTeamEvent` so `TeamSelector` no longer needs the internal contract.

### One gap PR 1 missed

**`GetTeamCustomRolesAsync` has no gated equivalent.** `ITeamManagementService` carries
`SetTeamCustomRolesAsync` and not its read. PR 1 filled five gaps found by surveying `.razor` files only;
this one lives in `TenantRoleManager` and `AuditLogView.razor.cs` was missed the same way. **Survey by
type, not by file extension** — the lesson, and the reason the count went from 8 to 11.

## Deliberately not in scope

- **`TeamMembershipClaimsBuilder` and `TeamClaimsAuthenticationStateProvider` stay on `ITeamService`.**
  They read team data *while building the principal*; a scope check there is circular and breaks sign-in.
- **The runtime backstop and the Roslyn analyzer** — separate features, both on the backlog.
- **Plan 01 §3b (startup registration sweep)** — related but independent.

## Acceptance criteria

1. A caller without `team:read` is refused team details, the roster and member lookups at every
   first-level surface — UI and MCP.
2. **A Viewer-level member is unaffected.** The no-op case stays a no-op; this is what makes the change
   safe for every consumer not using `Custom`, and it is the criterion most likely to break silently.
3. **No component, dialog or MCP provider injects `ITeamService`**, asserted by a test that fails
   *naming the offending type*.
4. **Sign-in and invitation acceptance both still work** — the two paths a naive gate breaks.
5. Full suite passes; the sample compiles and runs.

## Done condition

All five criteria met, `[EditorBrowsable(Never)]` and XML docs on `ITeamService`, `MAJOR_MINOR` at
`3.10`, a release note stating the behaviour change, `plan/` removed in the close-out commit, PR open
against `master`.

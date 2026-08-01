# Feature: Move every first-level surface onto the gated read path

**Branch:** `feature/gated-team-reads` (off `master`)
**Started:** 2026-08-01
**Release:** **`MAJOR_MINOR` 3.9 → 3.10.** Behaviour-changing: a caller lacking `team:read` that reads
today starts being refused. That is the fix, but a consumer relying on it must act.

## Goal

Close the `team:read` hole. The scope is registered at `AccessLevel.Viewer`, documented as *"View team
details and members"*, granted — and **enforced by nothing on any read**, because every read surface
injects `ITeamService`, the internal contract that is deliberately unchecked.

Invisible for ordinary members, since Viewer and above all hold it. It bites `AccessLevel.Custom` —
documented as *"least-privilege machine keys that should carry only their explicit grants"* — which today
reads team metadata, the full roster with access levels and states, and API-key metadata.

## What the survey found, and why this is bigger than "move the injections"

**Eight surfaces inject `ITeamService`.** What they call:

| Surface | Calls | Gated equivalent exists? |
|---|---|---|
| `TeamComponent` | `GetTeamAsync`, `GetTeamsAsync`, `GetAllTeamsAsync`, `SetTeamConsentAsync` | 2 of 4 |
| `TeamSelector` | `GetTeamsAsync`, `GetAllTeamsAsync`, `SelectTeamEvent` | 1 of 3 |
| `TeamsListView` | `GetTeamsAsync`, `GetAllTeamsAsync`, `DeleteTeamAsync`, `AssignOwnerAsync` | 2 of 4 |
| `UsersListView` | `GetTeamsAsync`, `GetAllTeamsAsync` | 1 of 2 |
| `TeamInviteView` | `GetTeamAsync`, `SetInvitationResponseAsync` | 2 of 2, but see below |
| `TeamDialog` | `RenameTeamAsync` | ✅ |
| `InviteUserDialog` | `AddMemberAsync` | ✅ |
| `AccessPage` (sample) | — | the worked example consumers copy |

**Five gaps in the gated surface have to be filled before anything can move:**

1. **`GetAllTeamsAsync`** — cross-team enumeration, already enforced on `teams:read` in the decorator, but
   with **no gated home**. It cannot go on `ITeamManagementService`: that interface is *wholly team-bound*
   (every method names a team in its first argument) and `shared-instructions.md` requires one
   registration to be true of every method. It is not `ITeamDirectoryService` either — that is the
   caller's *own* teams. **This needs a decision; see below.**
2. **`SetTeamConsentAsync`** — team-bound mutation, missing from `ITeamManagementService` while its
   siblings are all there.
3. **`AssignOwnerAsync`** — added to `ITeamService` in the previous feature and not to the gated
   interface. My omission, and it is exactly the drift this feature exists to stop.
4. **`GetInvitationAsync(inviteCode)`** — does not exist. `TeamInviteView` currently reads any team by
   naming its key and matches the code in memory. An invitee is not yet a member and holds no scope, so
   this entry point is authorized by the **code**, not by a scope.
5. **`SelectTeamEvent`** — an event on `ITeamService` that `TeamSelector` subscribes to. Needs a home
   that is not the internal contract.

## Deliberately not in scope

- **The runtime backstop** (`Tharga.MongoDB` `ICollectionInterceptor`) — cross-package, its own feature.
- **The Roslyn analyzer** — the only thing that protects *consumer* projects; on the backlog.
- **`TeamMembershipClaimsBuilder` and `TeamClaimsAuthenticationStateProvider` stay on `ITeamService`.**
  They read team data *while building the principal*; requiring a scope there is circular and breaks
  sign-in. They are already correct.

## Acceptance criteria

1. A caller without `team:read` is refused team details, the roster and member lookups at every
   first-level surface — UI and MCP.
2. **A Viewer-level member is unaffected.** The no-op case stays a no-op; that is what makes this safe
   for every consumer who is not using `Custom`.
3. **No component, dialog or MCP provider injects `ITeamService`**, asserted by an architecture test that
   fails naming the offending type.
4. **Sign-in and invitation acceptance both still work** — the two paths a naive gate breaks.
5. Full suite passes; the sample compiles and runs.

## Open decisions — these change the shape, so settle before coding

- **Where does `GetAllTeamsAsync` live?** A new wholly-system-wide gated interface (precedent:
  `ISystemApiKeyManagementService`), or something else.
- **Does the sample's `AccessPage` move?** It is the worked example consumers copy, so leaving it
  undercuts the architecture test — but it is the sample, not the product.
- **`[EditorBrowsable(Never)]` on `ITeamService`?** Non-breaking, hides it from IntelliSense, prevents
  the honest mistake. Costs discoverability for hosts that legitimately implement it.

## Done condition

All five criteria met, docs updated on both surfaces with an "inject this, not that" table, `plan/`
removed in the close-out commit, PR open against `master`.

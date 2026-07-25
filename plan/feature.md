# Feature: Team member visibility without `users:manage`

Closes Tharga/Platform#139 and Tharga/Platform#140.

## Goal

An ordinary user — one who holds no system scopes — must be able to open `<TeamComponent>`,
see their own teams, create a team and become its owner, and see who the members of their
teams actually are. Today the page throws before first render, and the consent drop-down on
the page offers an edit the server then rejects.

## Background

`users:manage` is a **system** scope, mapped from system roles by the host via
`o.ConfigureSystemRoles`. Team access level never grants it: `ScopeRegistry.GetScopesForAccessLevel`
hands Owner/Administrator every registered *team* scope and does not touch the system registry.
So a team owner is, and should remain, without `users:manage`.

Two consequences, filed as the two issues:

1. `TeamComponent.OnInitializedAsync` loads the entire user directory unconditionally
   (`UserService.GetAsync()`, `[RequireScope(users:manage)]`). Any caller without the scope gets an
   unhandled `UnauthorizedAccessException` and the page never renders (#139).
2. Merely gating that load is not enough. The member grid resolves each row's email, avatar and
   display name through the loaded directory, and `ITeamMember` carries no email of its own.
   Accepting an invitation *clears* `Member.Name` and promotes it to `User.Name`
   (`TeamServiceBase.SetInvitationResponseAsync`), so after acceptance both name and email come
   exclusively from the user record. An owner would see accepted members as "Unknown" with a
   blank email — the roster becomes unreadable.
3. `TeamActionGate.CanEditConsent` is the only gate in the class that does not take
   `(selectedTeamKey, teamKey)`. `team:manage` is issued for the selected team only, so the
   drop-down renders enabled on unmanaged teams and the service throws on change (#140) — the
   same defect class as #125 and #134.

## Scope

- Gate the directory load in `TeamComponent` on the `users:manage` claim.
- Add a caller-scoped user lookup so members without the system scope still resolve their
  co-members' identities.
- Require the team to be selected before the consent drop-down is editable.

Out of scope: any change to how system scopes are granted, and any new host configuration.

## Decisions

- **Co-member emails are visible to fellow team members, unconditionally.** No opt-in option.
  The grid has always displayed them; the visibility set is the caller's own teams.
- **The lookup takes no parameters.** `GetTeamMemberUsersAsync()` returns every user sharing at
  least one team with the caller, plus the caller. With nothing to pass in there is nothing to
  spoof, and authorization reduces to "is authenticated".
- **It is implemented in `AuthorizationUserServiceDecorator`.** The decorator holds the only
  handle on the *undecorated* inner service; anything else resolving `IUserService` from DI gets
  the decorator and would re-trigger the scope check. `ITeamService` is resolved lazily
  (`Func<ITeamService>` supplied by the registration factory) because
  `TeamServiceBase(IUserService …)` means constructor-injecting it would close a DI cycle.
- **Consent gates on the manage scope, not just access level.** `SetTeamConsentAsync` is enforced
  on `team:manage`, so the gate mirrors `CanDelete`'s shape rather than only adding the selected
  check the issue suggested.

## Acceptance criteria

- [ ] A user with no system scopes can open `<TeamComponent>` without an exception.
- [ ] That user can create their first team and appears as its owner with their own email and avatar.
- [ ] A team owner without `users:manage` sees every member of their teams with the correct email,
      name and avatar — including members who have accepted an invitation.
- [ ] A user sees no user record for anyone they share no team with.
- [ ] A caller holding `users:manage` still sees the full directory, unchanged.
- [ ] The consent drop-down is read-only on any team that is not the selected, managed one.
- [ ] Full test suite passes.

## Done condition

Both issues verifiable as fixed against the sample app, tests green, docs reviewed, PR opened
with release-note-quality description.

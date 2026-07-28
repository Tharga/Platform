# Feature: Delete a team from the Teams tab

## Goal

A system administrator can delete any team from `<UsersView />` → Teams, the same way they can already
delete a user from the Users tab. The capability is a system-user feature, gated by the
`teams:delete` system scope — never by a team's consent.

## Background

The server side is already complete and needs no change:

- `SystemTeamScopes.Delete` is registered by default in `ThargaBlazorRegistration` — *"Delete any team
  (cross-team), regardless of membership or the `AllowTeamCreation` option."*
- `ITeamService.DeleteTeamAsync<TMember>` is the operation, and
  `AuthorizationTeamServiceDecorator.RequireDeleteAsync` already admits a holder of
  `SystemTeamScopes.Delete` for any team.

Two gaps produce the reported behaviour:

1. **`TeamsListView` offers no delete action.** The Teams tab renders only a View button, while
   `UsersListView` carries a full split button including Delete. The tab was never given the equivalent.
2. **The only built-in grant of a system team scope runs through the consent block.**
   `ThargaTeamRegistration` grants `SystemTeamScopes.Read` from `Blazor.Consent.GrantTeamsRead` +
   `Consent.Roles`. `ConsentOptions` itself notes that `Roles` means "roles a team may grant access to" —
   a per-team inbound opt-in — so deriving a global privilege from it is a category error. Nothing grants
   `SystemTeamScopes.Delete` to a role at all.

Team deletion is a property of the operator, not something a team consents to. Consent governs what a
team exposes inbound; it must not decide who may destroy it.

## Scope

- Add a Delete action to each team row on the Teams tab of `TeamsListView`, shown only to a caller
  holding `SystemTeamScopes.Delete`, gated through `TeamScopeGate.HasSystemScope` (never a bare
  `HasClaim`).
- Confirm before deleting, naming the team and its member count.
- Delete via `ITeamService.DeleteTeamAsync<TMember>` — the already-authorized path. No new service, no
  new domain surface.
- Report success and failure through `NotificationService`, and reload the grid.
- Wire the sample host to grant `Developer` and `Administrator` the `teams:read` + `teams:delete` system
  scopes via the existing `ConfigureSystemRoles`, demonstrating the intended non-consent grant.
- Document the capability and how to grant it.

## Out of scope

- **No new configuration option.** Hosts grant the scopes with the existing `ConfigureSystemRoles`;
  a dedicated `SystemAdminRoles` option was considered and declined.
- **No change to `Consent.GrantTeamsRead`.** It stays as-is; decoupling or obsoleting it is a separate
  decision.
- No change to the authorization decorator, scopes, or claims pipeline.
- No change to `TeamComponent`'s own team-admin delete button.

## Acceptance criteria

- [ ] The Teams tab shows Delete only when the caller holds `SystemTeamScopes.Delete`; a caller with
      `users:manage` but not `team:delete` sees the tab and no Delete action.
- [ ] Gating goes through `TeamScopeGate.HasSystemScope`, not a bare `HasClaim`.
- [ ] Deleting removes the team, reloads the grid, clears the drill-down if it was the deleted team, and
      raises a success notification.
- [ ] A failed delete surfaces an error notification and leaves the grid intact.
- [ ] Consent state has no bearing on whether Delete is offered or succeeds.
- [ ] The sample grants the scopes via `ConfigureSystemRoles`, not via the consent block.
- [ ] Docs describe the capability and the grant.
- [ ] Full test suite passes.

## Done condition

Merged via PR, with the PR description spelling out for package consumers what the new action is and
which scope grants it.

## Version

Additive and opt-in — no public API is added and nothing breaks, so `MAJOR_MINOR` stays at `3.6` and this
ships as a patch. Existing hosts see no change until they grant `teams:delete`.

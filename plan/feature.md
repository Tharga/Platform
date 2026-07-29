# Feature: Admin views — team deletion, identity, and activity

## Goal

Make `<UsersView />` usable as a real operator surface. Today its two tabs list names and counts but
answer almost none of the questions an administrator actually arrives with: which row is me, what is this
record's key, who owns this team, is it still in use, and what happened to it.

## Scope

Grouped by the component they land on. Everything below draws on data the components already load —
no new service calls, no new queries, no interface changes.

### Both tabs

1. **Record keys.** Show `UserViewModel.Key` and `TeamViewModel.Key` in the existing per-row detail
   panel, each with copy-to-clipboard. A key that must be transcribed by eye is the reason to show it.
2. **`Never` for a null `LastSeen`.** A blank cell reads as "unknown"; `Never` reads as "never used".
3. **Audit history button** (opt-in `[Parameter]`). Opens a dialog hosting
   `<AuditLogView PinnedFilter="…" />` — pinned to `TeamKey` for a team row, `CallerIdentity` for a user
   row. Mirrors the `ShowAuditLogButton` pattern already on `ApiKeyView`.
4. **Cross-navigation.** Clicking a team in a user's detail panel switches to the Teams tab with that
   team expanded; clicking a member in a team's detail panel does the reverse.

### Users tab

5. **Highlight the signed-in user's row**, reusing `MemberHighlight.IsCurrentMember` and the
   `data-tharga-current-member` marker + cell-level `<style>` mechanism from `TeamComponent`.

### Teams tab

6. **Delete a team** — done; gated by the `teams:delete` system scope. See "Deleting teams" below.
7. **Last used** — the most recent `LastSeen` across the team's members. `ITeamMember.LastSeen` tracks
   *team selection*, so the maximum genuinely reads as "when anyone last used this team". Distinct from
   `IUser.LastSeen`, which is last authenticated request.
8. **Owner** — the member at `AccessLevel.Owner`. On a tab that can delete teams, "whose team is this"
   is the question the grid currently cannot answer. Also surfaces ownerless teams.
9. **Pending invitations** — split the member count so `MembershipState.Invited` rows are visible. A team
   showing "5" may be 1 member and 4 abandoned invitations, which is exactly what a delete decision turns on.
10. **Team avatar** — `ITeam.Icon` and `TeamAvatar` both exist; `TeamViewModel` simply never carried the
    icon. The Users grid shows avatars and the Teams grid shows none.
11. **Empty-team badge** — zero members. The orphans an operator with a Delete button is looking for.

## Deleting teams (the original scope, already implemented)

The server side needed no change: `SystemTeamScopes.Delete` (`teams:delete`) was already registered by
default and already honoured by `AuthorizationTeamServiceDecorator.RequireDeleteAsync`. The gaps were
that `TeamsListView` rendered no delete action and nothing granted the scope to a role.

Deletion is gated on the **system** scope, resolved through `TeamScopeGate.HasSystemScope` — never a
bare `HasClaim`, so an in-team grant of the same name cannot open it. Deliberately independent of
consent: consent governs what a team exposes inbound, not who may destroy it.

## Out of scope

- **No new configuration option** for the delete grant — hosts use the existing `ConfigureSystemRoles`.
- **No change to `Consent.GrantTeamsRead`.**
- **No team `Created` date.** `ITeam` carries no creation timestamp; adding one is an opt-in interface
  change of a different kind from everything above.
- No change to the authorization decorator, scopes, or claims pipeline.
- The Teams tab's existing `users:manage` requirement for *viewing* is untouched.

## Acceptance criteria

- [ ] Delete appears on the Teams tab only for a holder of `teams:delete`, and consent state does not
      affect whether it is offered or succeeds.
- [ ] The signed-in user's row is visually distinct on the Users grid, and no row is highlighted when two
      members carry a null key.
- [ ] Both detail panels show the record key with a working copy control.
- [ ] The Teams grid shows Last used, Owner, avatar, an invited-count split, and an empty-team badge.
- [ ] A null `LastSeen` renders as `Never` on both grids.
- [ ] The audit button is off by default and, when enabled, opens a log pinned to that team or user.
- [ ] Cross-navigation moves between tabs with the target row expanded.
- [ ] Every new decision is a pure, unit-tested helper — the project has no bUnit, so logic left in
      markup is unreachable from tests.
- [ ] Full test suite passes.

## Done condition

Merged via PR, with the PR description written as release notes: what each addition is, and that the
delete action requires `teams:delete` granted through `ConfigureSystemRoles`.

## Version

Additive. New opt-in `[Parameter]`s are added to existing components, so this is public API growth
without a break — `MAJOR_MINOR` moves `3.6` → `3.7`, and existing hosts see no behaviour change until
they grant `teams:delete` or set the audit parameter.

# Feature: Oversight defects — misleading denial, blind team counts, stale grid

**Target release:** 3.6.x (non-breaking)
**Issues:** [#149](https://github.com/Tharga/Platform/issues/149), [#150](https://github.com/Tharga/Platform/issues/150), [#151](https://github.com/Tharga/Platform/issues/151)

## Goal

Three defects filed 2026-07-27 against shipped 3.5.2, all surfacing on oversight and user-administration
surfaces. None is covered by PR #152. Each is small, localized, and independently verifiable.

## Defect 1 — `AccessLevelProxy` reports "No team selected" for a denied team (#149)

`AccessLevelProxy.CheckAccessLevel` (`Tharga.Team.Service/AccessLevelProxy.cs:104`) keys only off the
`TeamClaimTypes.TeamKey` claim — the *access anchor*, emitted only once access resolves. It never consults
the `team_id` selection marker, so "a team is selected but the caller has no access to it" is reported with
the same message as "no team is selected at all".

An oversight caller (`teams:read`) selecting a team that has consented them nothing hits
`UnauthorizedAccessException("No team selected.")`, which is simply untrue — the marker holds the key.

The two states must be distinguishable in the message.

## Defect 2 — `UsersListView` team counts ignore oversight (#150)

`UsersListView.LoadDataAsync` (`Tharga.Team.Blazor/Features/User/UsersListView.razor:156`) always calls
`TeamService.GetTeamsAsync<TMember>()`, which resolves the current user and returns *their* memberships:

```csharp
_teams = await TeamService.GetTeamsAsync<TMember>().ToArrayAsync();   // caller's member teams only
```

The per-user lookup built from `_teams` is therefore blind to any team the caller does not belong to, so an
admin with `users:manage` + `teams:read` sees **0** teams for such users. The sibling `TeamsListView` — the
Teams tab of the *same* `<UsersView>` — already branches on `TeamVisibility.CanSeeAllTeams` and renders those
teams correctly, so one tab contradicts the other.

## Defect 3 — `DirectoryOnlyUsersView` grid never refreshes (#151)

`DirectoryOnlyUsersView` (`Tharga.Team.Blazor/Features/User/DirectoryOnlyUsersView.razor:57`) binds
`RadzenDataGrid.Data` to a `readonly List<DirectoryUser>` that lives for the component's lifetime and is
mutated in place. The grid keys its internal view off the `Data` reference, so it keeps the empty view built
on first render. The caption is a plain `@_users.Count` read and so is correct — count right, grid empty.

The in-loop `if (_users.Count % 25 == 0) StateHasChanged()` never fires for fewer than 25 results, and
`StateHasChanged` alone would not rebuild the grid's view regardless.

## Acceptance criteria

- [ ] A caller with a team selected but no access to it gets a denial message that says so, not
      "No team selected."
- [ ] A caller with no team selected still gets "No team selected."
- [ ] An oversight caller (`users:manage` + `teams:read`) sees full team counts and memberships on the Users
      tab, matching what the Teams tab of the same component shows.
- [ ] A non-oversight caller's Users tab is unchanged — still their own teams only.
- [ ] The directory-only grid renders its rows whenever the caption reports a non-zero count, including for
      fewer than 25 results.
- [ ] Full test suite passes.

## Done condition

All three issues closed with tests that fail against the current code, docs updated where any described
behaviour changes, and the issue numbers referenced in the PR description.

## Out of scope

- Widening what an oversight caller may *do* with a team. #150 is a display fix: the Users tab already
  requires `users:manage` to render, and the teams it will now count are ones the Teams tab already lists.
  No authorization boundary moves.
- Reworking `DirectoryOnlyUsersView`'s streaming/batching design. Only the refresh defect is in scope.

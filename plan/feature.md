# Feature: Teams-tab action templates on `UsersView`

## Goal

Close the second half of PlutusWave's `UsersView` extension request (`Requests.md` › Tharga.Team,
2026-07-30). The 2026-04-29 split delivered row-action hooks on the child components, but the
`UsersView<TMember>` wrapper only forwards the **Users**-tab hooks. A host rendering the packaged
`<UsersView TMember="..." />` therefore gets a Teams tab it cannot extend, and the only way to add a team
row action is to abandon the wrapper and re-implement the tab shell — including the directory-only tab the
wrapper's own documentation advertises. That is precisely the forking the split request set out to remove.

## Scope

### 1. Forward the Teams-tab templates (the request's ask #1)

`TeamsListView<TMember>` already exposes both hooks:

- `TeamActionsTemplate` (`RenderFragment<TeamViewModel>`) — rendered after the built-in View button in each
  team row's action column.
- `MemberActionsTemplate` (`RenderFragment<TeamMemberInfo>`) — rendered in the member drill-down.

`UsersView.razor` passes neither. Add both as `[Parameter]`s on the wrapper and forward them, symmetric
with the existing users-side pass-through (`ActionsTemplate`, `ActionItems`, `ActionInvoked`).

### 2. Make the admin-surface scopes clear in the documentation

Widened from the request's ask #3 on the user's instruction. Deleting a team from the Teams tab needs
**three** system scopes doing three different jobs — `users:manage` to see the surface, `teams:read` to see
teams you are not a member of, `teams:delete` for the action and the service call — and that is not
guessable from any current document. Deleting a *user* needs only `users:manage`. Document the whole
matrix, not just the one correction below.

### 2b. Correct the `ConfigureSystemScopes` documentation (the request's ask #3)

`ConfigureSystemScopes` is widely described — including in `Requests.md` itself — as what makes a scope
grantable to a system API key. But `ThargaBlazorRegistration` auto-registers `teams:delete` and
`users:manage` into the system scope registry regardless, so omitting them from `ConfigureSystemScopes`
does **not** withhold them from keys. Consumers have written comments and tests asserting an isolation
that does not hold.

Document the auto-registration where consumers actually read: the implementation guide and the Blazor
README. Prefer documenting over changing the registry — the auto-registration is deliberate (the admin
surfaces need those scopes to be grantable), and narrowing it now would be a silent behaviour break for
anyone already relying on it.

### 3. Folded in from testing feedback (2026-07-30)

- **Identity and directory id in the user detail panel.** `UserViewModel` carried neither, though `IUser`
  exposes both and the sample entity already declares `DirectoryId`. Added alongside the key, each with a
  copy control. A missing directory id distinguishes **Not stored** (entity does not declare it) from
  **Not resolved yet** via the new pure `DirectoryLink` helper — an empty value would read as "no
  directory account", a third and wrong meaning.
- **Teams tab row actions are now a split button**, matching the Users tab, and gained
  `TeamActionItems` / `TeamActionInvoked` (+ `TeamRowAction`). A split button needs item-injection, not
  just a trailing template — without it a host could not put an action *inside* the menu, which is where
  the Users tab puts them. Action column narrows 200px → 140px.
- **The sample's "Edit" is now "Edit email".** It overlapped the built-in Rename on the name field,
  teaching a duplicate from a file that doubles as documentation. It now edits only the field the toolkit
  has no built-in editor for, and passes `StoredName` through untouched so a resolved display fallback is
  never promoted into a stored name.

## Out of scope

- **The request's ask #2** — a built-in Delete action on the Teams tab — **shipped in 3.7.0** (PR #162),
  gated on the system `teams:delete` scope via `TeamScopeGate.HasSystemScope`, deliberately *not* reusing
  the `TeamActionGate` narrowing the request warned against.
- **Changing what `ConfigureSystemScopes` registers.** Documented, not narrowed — see above.
- **GitHub #155** (role badges on the profile page). Unrelated surface; keep this release to one request.

## Acceptance criteria

- [ ] `UsersView` exposes and forwards `TeamActionsTemplate` and `MemberActionsTemplate`.
- [ ] A host rendering the bare `<UsersView TMember="..." />` can add a team row action without forking.
- [ ] Both new parameters carry XML documentation matching the existing users-side wording.
- [ ] The system scopes gating each `<UsersView />` action are documented as a table, including that they
      must be **system** grants and that `teams:read` may arrive via `Consent.GrantTeamsRead`.
- [ ] The auto-registration of `teams:delete` / `users:manage` is documented on both doc surfaces.
- [ ] Existing `UsersView` consumers are unaffected when the new parameters are unset.
- [ ] Full test suite passes.

## Done condition

Merged via PR, with the PR description written as release notes. On close-out, the PlutusWave request in
`Requests.md` is marked **Done** — ask #2 delivered in 3.7.0, asks #1 and #3 in this release — with the
consumer follow-up entry added under `## Follow-up`.

## Version

Additive. Two new optional `[Parameter]`s on an existing component and a documentation correction; no
behaviour change when unset. This is a **patch** on the 3.7 line — `MAJOR_MINOR` stays `3.7`, and CI
auto-increments the patch from the git tags.

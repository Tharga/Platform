# Feature: Cross-team visibility for oversight roles

## Goal

Let a host grant designated roles (typically Developer / Administrator) the ability to **see every
team**, while what they can *do* inside a team stays governed by that team's own consent setting.
Surface each team's consent level visually so an oversight user can tell at a glance how much access
they have.

## Model

Two separate concerns, deliberately:

- **Discovery is global.** A caller holding the new `teams:read` system scope enumerates all teams.
- **Access is per-team and consent-governed.** Selecting a team they aren't a member of grants the
  scopes that team consented to — no consent, no access. Unchanged from today's behaviour.

This keeps tenant data behind the tenant's own opt-in while making the estate visible for support
and administration.

## Background — what exists today

- `GetTeamsAsync()` is membership-only for every caller, at every layer, with no role/scope branch
  and no configuration (`TeamServiceRepositoryBase` → `_teamRepository.GetTeamsByUserAsync(user.Key)`).
- The only toolkit-enforced system team scope is `teams:delete`. `system:teams:read` appears in the
  sample and tests but **nothing reads it** — it is decorative.
- The non-member consent branch **already works** (`TeamServerClaimsTransformation.cs:106-134`): a
  user whose role is in the team's `ConsentedRoles` receives `ConsentAccessLevel` and its scopes. It
  simply never fires, because an unreachable team can't be selected. Fixing discovery activates it.
- #125 (merged, unreleased) already renders the consent selector **visible but disabled** below
  `AccessLevel.Administrator`, and `HasAccessLevel` is `false` for non-members — so read-only consent
  display for an oversight user is already in place once the team is reachable.

## Scope

1. **`SystemTeamScopes.Read = "teams:read"`** — a real, enforced constant alongside `Delete`.
2. **Opt-in convenience flag** `o.Blazor.Consent.GrantTeamsRead` (default `false`): when set, the
   roles in `Consent.Roles` are also granted `teams:read`. Default-off deliberately — auto-deriving
   would silently escalate privileges for existing hosts on upgrade.
3. **Service layer** — `ITeamService.GetAllTeamsAsync()`, gated on `teams:read` in
   `AuthorizationTeamServiceDecorator`; **not** audited (explicit decision — mutations remain audited).
4. **Team-list resolution** — one pure, testable helper deciding all-teams vs own-teams, used by
   every listing surface rather than triplicating the scope check.
5. **Consent badge** on `TeamComponent` and `TeamsListView`, plus a tinted dot on `TeamSelector`.
   Visible only to `teams:read` holders. Text + theme-aware tint, never colour alone.

## Non-goals

- Changing what `GetTeamsAsync()` returns. It stays membership-scoped; the widened path is a
  separate method, so consumer code asking "which teams am I in?" keeps working.
- Auditing team enumeration (user decision).
- Per-role consent granularity. `SetConsent` writes one level for all configured consent roles.

## Acceptance criteria

- [ ] `teams:read` is defined in `SystemTeamScopes` and enforced in the authorization decorator.
- [ ] `GrantTeamsRead = false` (default) leaves behaviour byte-for-byte unchanged.
- [ ] `GrantTeamsRead = true` grants `teams:read` to every role in `Consent.Roles`.
- [ ] A holder of `teams:read` sees all teams in `TeamComponent`, `TeamsListView` and `TeamSelector`.
- [ ] A non-holder sees exactly the teams they belong to — no regression.
- [ ] Selecting a non-member team yields that team's consented scopes; a team with no consent yields
      no team access and does not error.
- [ ] Consent level is shown to `teams:read` holders as text plus a theme-aware tint, gated on the
      **scope**, not a hard-coded role name.
- [ ] Adding the feature does not break consumers deriving from `TeamServiceBase` /
      `TeamServiceRepositoryBase`, or implementing `ITeamRepository<,>`.
- [ ] Full suite green; `dotnet build -c Release` clean.
- [ ] `README.md` and `docs/articles/implementation-guide.md` updated.

## Done condition

All acceptance criteria met, user has tested from the pushed branch and confirmed, then close-out per
the Feature Workflow.

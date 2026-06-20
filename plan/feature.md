# Feature: LoginDisplay — gate the Team menu item by role

## Goal
Add an opt-in way to hide the **Team** item in `LoginDisplay`'s profile menu from users who are
not in a configured role. Default behavior is unchanged — the Team item stays visible exactly as
today when no roles are configured.

GitHub issue: Tharga/Platform#100. Consumer: Eplicta FortDocs (team management should be admin-only;
today only the `/team` page is `[Authorize]`-gated, so non-admins still see the menu link).

## Scope
- New `[Parameter] public string[] TeamMenuRoles` on `LoginDisplay` (plural — any-of match, so e.g.
  both `Administrator` and `Developer` can be allowed).
- `null`/empty → no role restriction (today's behavior: visible when a team service is registered or
  `ShowTeam=true`).
- When set → Team item shown only if the authenticated user is in **at least one** listed role
  **and** the existing visibility (`ShowTeam ?? teamServiceRegistered`) is true.
- Pure, unit-testable decision method (this test project uses reflection/unit tests, no bUnit).
- Bundled dependency bumps (patch) for Tharga.Team as the first step.

## Out of scope (issue mentions, user narrowed to role)
- Scope-based and AccessLevel-based gating — possible follow-ups.

## Acceptance criteria
- [ ] `TeamMenuRoles` parameter exists (string[], default null) on `LoginDisplay`.
- [ ] No roles configured → Team item visibility identical to current behavior.
- [ ] Roles configured → visible only when user is in any listed role and base visibility is true.
- [ ] Empty/whitespace entries ignored; `ShowTeam=false` still hides regardless of roles.
- [ ] Unit tests cover the decision matrix + the parameter default.
- [ ] Docs updated (implementation-guide LoginDisplay row + usage note).
- [ ] Build + full test suite green on net9.0 and net10.0.

## Done condition
PR opened `feature/login-display-team-role` → `master`; user confirms after testing.

# Feature: Team Consent for Developer/Admin Viewer Access (Request #11)

## Goal
Allow teams to grant consent for users with specific global roles (e.g. Developer, SystemAdministrator) to access the team as viewers.

## Scope

### Data model
- Add `ConsentedRoles: string[]` (or similar) to team entity
- Persist consent state per team

### UI — consent toggle
- Visible to team administrators in TeamComponent
- Toggle label states which global roles will gain viewer access
- `ShowConsentToggle` option (default true) — can hide the toggle entirely
- `DefaultConsent` option (default false) — new teams start with consent on/off

### Configuration
- `ConsentRoles: string[]` — which global roles can receive consent (e.g. `["Developer", "SystemAdministrator"]`)
- Configured via `ThargaBlazorOptions` or `ThargaPlatformOptions`

### Auth behavior
- Users with a consented global role see the team in their team selector
- Access level is viewer (read-only)
- Global roles are set directly on user claims, not team-level

- From: Tharga.Platform (self-request) — Priority: Medium

## Acceptance Criteria
- [ ] Consent toggle visible to team admins
- [ ] Toggle label shows which roles gain access
- [ ] Consented teams appear in team selector for users with matching global roles
- [ ] Viewer-level access only
- [ ] `ShowConsentToggle`, `DefaultConsent`, `ConsentRoles` configurable
- [ ] Tests cover consent grant/revoke and team selector filtering

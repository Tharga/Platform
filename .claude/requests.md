# Feature Requests

## Pending

### Document CreateTeam/CreateTeamMember patterns with Invitation and Name
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** The `TeamServiceRepositoryBase` requires consumers to override `CreateTeam` and `CreateTeamMember`, but documentation doesn't show the full pattern including `Invitation` and `Name` fields. Without it, consumers miss creating the `Invitation` object (with `InviteKey`, `EMail`, `InviteTime`) resulting in no "copy invite link" button, and miss setting `Name` on the member resulting in empty member list entries.
  Also consider: should Platform generate the `Invitation` object automatically in `AddMemberAsync` instead of requiring the consumer to do it? The invite key generation and email assignment are boilerplate that every consumer must replicate.
- **Status:** Pending

### Improve UsersView with Gravatar, filters, tabs, and team membership
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** The current `UsersView` is very technical — raw keys, no avatars, no filtering. Redesign with users tab, teams tab, and cross-reference.
- **Status:** Pending

### Team consent for developer/admin viewer access
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** Teams should be able to grant consent for global-role users (e.g. `Developer`, `SystemAdministrator`) to access the team as viewers.
- **Status:** Pending

## Notifications

### Option to disable team creation — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** Added `AllowTeamCreation` bool to `ThargaBlazorOptions` (default true). When false, hides both "Create team" and "Delete team" buttons in TeamComponent.
- **Branch/Version:** develop

### SSR claims enrichment — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** `TeamServerClaimsTransformation` (IClaimsTransformation) auto-registered by `AddThargaTeamBlazor`. Reads `selected_team_id` cookie and enriches claims server-side with team, role, access level, and scope claims. `SkipAuthStateDecoration` now defaults to `true`. No manual IClaimsTransformation needed.
- **Branch/Version:** develop

### TeamServiceBase should use GetEmail() — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** `ResolveDisplayName` prefers `IUser.Name` (from `name` claim), falls back to email parsing with title case. Added `IUser.Name` as optional default interface member. `displayName` passed to abstract `CreateTeam`.
- **Branch/Version:** develop

### CreateTeam accept display name — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** Merged with GetEmail fix. `CreateTeam` now receives `string displayName` parameter. `IUser.Name` added.
- **Branch/Version:** develop

### API/Audit access control — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** `CrossTeamRoles` and `RequiredScopes` parameters added to `ApiKeyView` and `AuditLogView`. Developer role grants cross-team access by default. AuditLogView shows "No team selected" when appropriate.
- **Branch/Version:** develop

### Audit page "No team selected" — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** Merged with API/Audit access control fix above.
- **Branch/Version:** develop

### API key fixes (scope defaults + ownership bug) — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** `GetKeysAsync` now filters by team key. Simple mode hides expiry/scope columns. Default access level corrected.
- **Branch/Version:** develop

### Copy claims as JSON — DONE
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Completed:** 2026-03-30
- **Summary:** Copy as JSON button added to UserProfileView claims section. JSON includes identity name, auth type, and all claims.
- **Branch/Version:** develop

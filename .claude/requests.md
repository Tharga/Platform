# Feature Requests

## Pending

### Option to disable team creation for users
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** `ThargaBlazorOptions` needs an option to prevent users from creating teams (e.g. `AllowTeamCreation = false`). In FortDocs, teams (farms) are provisioned by administrators — users should not be able to create their own. Currently the "Create team" button in `TeamComponent` is always shown with no way to hide it. Suggested implementation: add `bool AllowTeamCreation { get; set; } = true` to `ThargaBlazorOptions` and conditionally render the "Create team" button based on this option.
- **Status:** Pending

### SSR apps need built-in claims enrichment for team roles and scopes
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** High
- **Description:** SSR Blazor apps must set `SkipAuthStateDecoration = true` because `TeamClaimsAuthenticationStateProvider` uses JS interop (LocalStorage) which crashes during SSR prerendering. But with that flag enabled, there is no Platform-provided mechanism to enrich claims with team roles (`TeamMember`, `Team{AccessLevel}`), access level, or scope claims on the server side. Every SSR app must write its own middleware to replicate what `TeamClaimsAuthenticationStateProvider` does for WASM. Suggested fix: when `SkipAuthStateDecoration = true`, automatically register a server-side middleware (or `IClaimsTransformation` with proper re-entrance guards) that reads the `selected_team_id` cookie, looks up the member, and adds role/access-level/scope claims. This should be built into `AddThargaTeamBlazor()` or `UseThargaPlatform()`.
- **Status:** Pending

### TeamServiceBase should use GetEmail() when building default team name
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** `TeamServiceBase.CreateTeamAsync` (line 65) reads `user.EMail` to build the default team name. But the email stored on the user depends entirely on what the consumer sets in `CreateUserEntityAsync`. If the consumer uses `ClaimTypes.Email` and Entra ID doesn't emit that claim (it often uses `preferred_username` instead), the email is empty and the team gets named "Unknown's team". The same issue affects avatar display (no email → no Gravatar). Platform already has `ClaimsExtensionsStandard.GetEmail()` which checks `email`, `emails`, `preferred_username`, and `name` in order. **Suggested fix:** `UserServiceBase.GetUserAsync` (or the default team name builder) should use `GetEmail()` internally as a fallback, so the email is resolved correctly regardless of which claim the identity provider uses.
- **Status:** Pending

### Document CreateTeam/CreateTeamMember patterns with Invitation and Name
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** The `TeamServiceRepositoryBase` requires consumers to override `CreateTeam` and `CreateTeamMember`, but documentation doesn't show the full pattern including `Invitation` and `Name` fields. Without it, consumers miss creating the `Invitation` object (with `InviteKey`, `EMail`, `InviteTime`) resulting in no "copy invite link" button, and miss setting `Name` on the member resulting in empty member list entries. Add a documented example pattern like:
  ```csharp
  protected override Task<TeamMember> CreateTeamMember(InviteUserModel model)
  {
      return Task.FromResult(new TeamMember
      {
          Key = null, // assigned on registration
          Name = model.Name,
          Invitation = new Invitation
          {
              EMail = model.Email,
              InviteKey = Guid.NewGuid().ToString(),
              InviteTime = DateTime.UtcNow
          },
          State = MembershipState.Invited,
          AccessLevel = model.AccessLevel
      });
  }
  ```
  Also consider: should Platform generate the `Invitation` object automatically in `AddMemberAsync` instead of requiring the consumer to do it? The invite key generation and email assignment are boilerplate that every consumer must replicate.
- **Status:** Pending

### New API keys get wrong default scope and simple mode should hide advanced fields
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** Two issues with API key creation in `ApiKeyView`:
  1. **Default scope:** New API keys are created with `team:read` scope. They should default to "User" role access instead.
  2. **Simple/auto-create mode:** When simple API key handling is used (auto-create), the Expiry and Scope fields should be hidden since they are not relevant. Add an option like `SimpleApiKeyMode = true` on `ThargaBlazorOptions` to hide these fields.
- **Status:** Pending

### Bug: RefreshKeyAsync fails with "API key does not belong to team"
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** High
- **Description:** When trying to regenerate/refresh an API key, `ApiKeyAdministrationService.RefreshKeyAsync` throws: `Exception: API key does not belong to team 'XKK2KLRFG'`. The `VerifyTeamOwnership` check fails even though the key was created for that team. Stack trace: `VerifyTeamOwnership → RefreshKeyAsync → ApiKeyView.RefreshAsync`. This appears to be a bug in how the team ownership is stored or verified during key creation vs refresh.
- **Status:** Pending

### API and Audit menu items should be hidden when no team is selected
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Medium
- **Description:** The "API" and "Audit" navigation items are visible even when no team is selected. These features are team-scoped and have no meaningful content without a team context. They should be conditionally rendered based on whether a team is currently selected (i.e., check for `TeamKey` claim or `ITeamStateService.GetSelectedTeamAsync()` returning non-null). This could be built into the Platform's nav components, or exposed as a helper so consumers can conditionally render menu items.
- **Status:** Pending

### Audit page should show "No team selected" when no team is selected
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Low
- **Description:** The `ApiKeyView` component correctly shows "No team selected." when no team is active. The audit page (`/audit`) should show the same message instead of rendering an empty or broken view. Consistent behavior across team-scoped pages.
- **Status:** Pending

### CreateTeam should accept optional user display name
- **From:** Eplicta.FortDocs (`c:\dev\Eplicta\FortDocs`)
- **Date:** 2026-03-26
- **Priority:** Low
- **Description:** `TeamServiceRepositoryBase.CreateTeam(string teamKey, string name, IUser user)` provides the user but no way to set the owner member's display name. The `IUser` interface has `Key` and `EMail` but no `Name`. Consumers must leave the owner member's `Name` as null. **Suggested fix:** Either add `Name` to `IUser`, or pass an additional `string displayName` parameter (resolved from `name` claim or similar) to `CreateTeam` so the owner member can have a visible name in the member list.
- **Status:** Pending

### Copy claims and principal info to clipboard as JSON
- **From:** Tharga.Platform (`c:\dev\tharga\Toolkit\Platform`)
- **Date:** 2026-03-26
- **Priority:** Low
- **Description:** Add a copy button to the Claims section in `UserProfileView` that copies all claims and principal information to the clipboard as pretty-printed JSON. Useful for debugging authentication issues. The JSON should include the identity name, authentication type, and all claims (type + value).
- **Status:** Pending

## Notifications

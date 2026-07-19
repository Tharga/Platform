# Tharga Team Blazor
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Blazor)](https://www.nuget.org/packages/Tharga.Team.Blazor)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Blazor)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Team management Blazor components for multi-tenant applications. Works with both **Blazor Server** and **Blazor WebAssembly**.

## Components

- **Team management** - `TeamSelector`, `TeamComponent`, `TeamDialog`, `InviteUserDialog`, `TeamInviteView`.
- **API key management** - `ApiKeyView` for team-scoped API keys. Row actions are in a single overflow (`⋮`) **context menu** (copy, show/hide, audit, edit roles & scopes, lock, refresh, delete). On **create or regenerate** the key is shown once in a **reveal dialog** with a copy button and a "shown only once / not stored" warning — required because with `AutoLockKeys` the key is locked immediately. Shows **Created** and **Last used** columns per key (`SystemApiKeyView` shows the same for system keys); the Last used tooltip lists Created / Expiry / **Created by** (falling back to "System" for keys with no recorded creator, e.g. auto-generated). Also shows a **Tags** column (system-set key-value tags, displayed read-only via an `(i)` tooltip). Per-component parameters: `ShowScopeTooltip` (effective-scope `(i)`, default on), `ShowScopeOverrides` (scope-override editor), `ShowRoles` (tenant-role editor), `ShowLastUsed` (Last used column; 60-day expiry warning), `ShowExpiryDatePicker`, `ShowTags` (`bool?` — null = auto-show when any key has tags), `ChipTagKeys`, `ShowAuditLogButton`, `AllowGridSorting` (sort by Name / Last used, default on, Name ascending), `AllowGridFiltering` (case-insensitive Name text filter, default off), `ShowPrivateKeys` (`None`/`Mine`/`All` — include owner-scoped "private" keys; default None) and `AllowPrivilegedAccess` (let Administrator/Owner *see* private keys when `All`; view-only). `TeamComponent` shares `ShowScopeTooltip`/`ShowScopeOverrides`/`ShowRoles`; `SystemApiKeyView` uses global **system scopes** (`o.ConfigureSystemScopes`). Access is gated on the `apikey:manage` scope; cross-team access comes from mapping a role to system scopes (`o.ConfigureSystemRoles`), not per-component role parameters. "Last used" writes are throttled by `ApiKeyOptions.LastUsedThrottle` (default 1 min).
- **Scope explorer** - `ScopeView` shows which scopes a member would have: pick an **access level** (single-select bar; Owner/Administrator are merged since they grant the same scopes) and **roles** (multi-select bar), and scopes not granted by the selection are **greyed out**. Defaults to the signed-in member's own access level, roles, and **scope overrides** (overrides are highlighted with a ⭐ and an `Override` badge). Built dynamically from `IScopeRegistry` / `ITenantRoleRegistry`, so it always reflects the live configuration (no hard-coded list). Parameters: `ShowDescription` (default on), `ShowAccessLevelSelector` (default on), `ShowRoles` (default on; the roles bar auto-hides when no tenant roles are configured), `AllowGridSorting` (sort by scope name, default on), `AllowGridFiltering` (case-insensitive name filter, default off). Shows a friendly notice when no scopes are configured.
- **Custom role management** - `TenantRoleManager` lets a team administrator (`team:manage`) create / edit / delete the team's own **runtime-defined custom roles** — each granting a chosen subset of the app-registered scopes — without a code deploy. Requires `o.EnableDynamicRoles = true`. Scopes are picked from `IScopeRegistry` (so a role can never grant an unregistered scope), and the server rejects duplicate names or collisions with code-registered roles. Custom roles then appear alongside code roles in `TeamComponent`'s role picker. See "Dynamic (runtime-defined) tenant roles" below.
- **User management** - `UserProfileView`, `UsersView`.
- **Authentication** - `LoginDisplay` with login/logout and team navigation.
- **Claims augmentation** - `TeamClaimsAuthenticationStateProvider` adds `TeamKey`, `AccessLevel`, role, and scope claims. Compatible with all hosting models.
- **Scope enforcement in the circuit** - `AddThargaTeamBlazor` registers a circuit-aware `ITeamPrincipalAccessor`, so `[RequireScope]` / `[RequireAccessLevel]` on services (registered with `AddScopedWithScopes` / `AddScopedWithAccessLevel`) enforce when called from interactive Blazor Server components, not just from controllers/API.
- **Audit** - `AuditLogView` for viewing audit logs with charts and filtering.

## Quick Start (recommended)

Use `AddThargaPlatform` to register all Platform services in one call:

```csharp
builder.AddThargaPlatform(o =>
{
    o.Blazor.Title = "My App";
    o.Blazor.RegisterTeamService<MyTeamService, MyUserService>();
});

var app = builder.Build();
app.UseThargaPlatform();
```

This registers auth (Azure AD + OIDC), API key authentication, Blazor components, and controllers with sensible defaults. See the main [README](../README.md) for the full setup including MongoDB.

## Individual Registration

For partial or custom setups, use the individual methods:

### Authentication

```csharp
builder.AddThargaAuth();   // registers auth services
app.UseThargaAuth();       // maps /login and /logout endpoints
```

Requires an `AzureAd` section in `appsettings.json`:

```json
{
  "AzureAd": {
    "Authority": "https://<tenant>.ciamlogin.com/<domain>",
    "ClientId": "<client-id>",
    "TenantId": "<tenant-id>",
    "CallbackPath": "/signin-oidc"
  }
}
```

### Team management

```csharp
builder.Services.AddThargaTeamBlazor(o =>
{
    o.Title = "My App";
    o.RegisterTeamService<MyTeamService, MyUserService>();
});
```

**UI components:**
- `<LoginDisplay />` — profile menu with Gravatar when authenticated, login button when not.
- `<UserProfileView />` — displays the user's profile info and authentication claims.

### Per-team role visibility

When `TeamComponent`'s role editor is enabled (`ShowRoles`), it offers every registered tenant role by default. To hide feature-gated roles from teams where the feature is disabled, register an `ITenantRoleVisibilityProvider` (from `Tharga.Team`):

```csharp
public sealed class FeatureGatedRoleVisibility : ITenantRoleVisibilityProvider
{
    public async Task<bool> IsRoleVisibleAsync(string teamKey, string roleName, CancellationToken ct = default)
        => await _features.IsRoleEnabledForTeamAsync(teamKey, roleName, ct);
}

builder.Services.AddSingleton<ITenantRoleVisibilityProvider, FeatureGatedRoleVisibility>();
```

`TeamComponent` filters the editor's role list per team through the provider. Hiding a role is **UI-only**: a role already assigned to a member stays assigned (it is preserved, never pruned, and reappears if the feature is re-enabled) and continues to grant its scopes at runtime. The default provider shows all roles, so this is opt-in and non-breaking.

### Dynamic (runtime-defined) tenant roles

Code-registered roles (`o.ConfigureTenantRoles`) are the same for every team and require a deploy to change. **Dynamic tenant roles** let a team administrator define their own roles per team at runtime — for example, org-specific operational roles like Registrar / Case officer / Reader / Archivist — each granting a chosen subset of the app-registered scopes.

Enable the feature, then drop the management component on a `team:manage`-gated page:

```csharp
builder.AddThargaPlatform(o =>
{
    o.ConfigureScopes = s => { s.Register("case:read", AccessLevel.Custom); s.Register("case:write", AccessLevel.Custom); /* … */ };
    o.EnableDynamicRoles = true;   // registers the team-aware resolver + enables TenantRoleManager
    // o.DynamicRoleManageScope = "access:manage"; // optional — scope required to manage custom roles (default team:manage)
});
```

```razor
@attribute [Authorize]
<TenantRoleManager />
```

- **Storage & scope** — custom roles live on the team document (per team), created/edited/deleted via `ITeamManagementService.SetTeamCustomRolesAsync`, which requires `team:manage` on the team by default. Set `o.DynamicRoleManageScope` (e.g. `"access:manage"`) to gate custom-role CRUD under a dedicated scope instead — enforced by both the service layer and `TenantRoleManager`. *Assigning* a role to a member remains a `member:manage` operation.
- **No privilege escalation** — a custom role may only grant scopes registered via `o.ConfigureScopes`; the server rejects any unregistered scope, duplicate role names, and names that collide with code-registered roles.
- **Uniform surfacing** — when enabled, a member assigned a custom role receives that role's scopes as claims (server, WASM, and API-key paths). Custom roles also appear alongside code roles in the role pickers of `TeamComponent` (respecting `ITenantRoleVisibilityProvider`) and, with `ShowRoles="true"`, `ApiKeyView` — so a custom role can be assigned to a team API key.
- **Off by default** — with `EnableDynamicRoles = false` (the default) only code roles apply and behaviour is unchanged.

### Overriding the "Create team" action

By default a teamless user's **Create team** link (in `TeamSelector`) navigates to `/team`, and the **Create new Team** button (in `TeamComponent`) calls `ITeamManagementService.CreateTeamAsync()` directly. A host that wants team creation to run through its own onboarding flow (collect organization type, working language, seed templates, …) can override where these built-in entry points lead — **without** setting `AllowTeamCreation = false`, which hides the button but also blocks the programmatic create API.

Two override points, evaluated in this order (**callback → path → built-in**):

**1. `CreateTeamPath` (global, declarative).** Point the built-in entry points at your own page:

```csharp
builder.AddThargaPlatform(o =>
{
    o.Blazor.CreateTeamPath = "/get-started";   // TeamSelector link + TeamComponent button navigate here
});
```

Your `/get-started` page runs the wizard and calls `CreateTeamAsync()` itself (works because `AllowTeamCreation` stays `true`), then runs onboarding.

**2. `CreateTeamRequested` (per component, imperative).** Handle the create in place — e.g. open a dialog — and skip navigation entirely. Takes precedence over `CreateTeamPath`:

```razor
<TeamSelector CreateTeamRequested="LaunchOnboardingAsync" />
<TeamComponent TMember="MyMember" CreateTeamRequested="LaunchOnboardingAsync" />

@code {
    private async Task LaunchOnboardingAsync()
    {
        var team = await OnboardingWizard.RunAsync();   // your flow: collect info + CreateTeamAsync + seed
        // navigate / refresh as needed
    }
}
```

When neither is set, behavior is unchanged. `CreateTeamPath` is `null` and both `CreateTeamRequested` callbacks are unset by default, so this is additive and non-breaking. Note the override applies to the built-in UI entry points only; teams created programmatically or via `AutoCreateFirstTeam` are unaffected.

### Cross-team visibility for oversight roles

Support and administration roles usually need to see every team, not just the ones they belong to. The
`teams:read` system scope grants **discovery only** — access inside a team still depends on that team's
consent.

```csharp
builder.AddThargaPlatform(o =>
{
    // Explicit:
    o.ConfigureSystemRoles = roles => roles.Map("Developer", SystemTeamScopes.Read);

    // Or reuse the consent role list (opt-in, default false):
    o.Blazor.Consent.Roles = ["Developer"];
    o.Blazor.Consent.GrantTeamsRead = true;
});
```

`GrantTeamsRead` defaults to `false` deliberately: `Consent.Roles` means "roles a team may grant access
to", and silently promoting that to a global enumeration privilege would widen access for existing hosts
on upgrade.

A holder of `teams:read` sees every team in `TeamComponent`, `TeamSelector` and `UsersView` → Teams,
each tagged with what that team has consented to — **No access**, **Partial access** or **Full access** —
plus a **Not a member** badge where applicable. Selecting a team you don't belong to lasts for the
session only; automatic team selection always draws from your own memberships, so you are never
defaulted into someone else's team. Enumeration is not audited; mutations still are.

## Dependencies

- [Tharga.Blazor](https://www.nuget.org/packages/Tharga.Blazor) - Generic UI components.
- [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) - Domain models and authorization primitives.
- [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) - Audit types for AuditLogView.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams and users |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Server-side API key auth, Swagger, audit logging |

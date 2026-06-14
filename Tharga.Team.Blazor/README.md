# Tharga Team Blazor
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Blazor)](https://www.nuget.org/packages/Tharga.Team.Blazor)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Blazor)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Team management Blazor components for multi-tenant applications. Works with both **Blazor Server** and **Blazor WebAssembly**.

## Components

- **Team management** - `TeamSelector`, `TeamComponent`, `TeamDialog`, `InviteUserDialog`, `TeamInviteView`.
- **API key management** - `ApiKeyView` for team-scoped API keys. Row actions are in a single overflow (`⋮`) **context menu** (copy, show/hide, audit, edit roles & scopes, lock, refresh, delete). On **create or regenerate** the key is shown once in a **reveal dialog** with a copy button and a "shown only once / not stored" warning — required because with `AutoLockKeys` the key is locked immediately. Shows **Created** and **Last used** columns per key (`SystemApiKeyView` shows the same for system keys); the Last used tooltip lists Created / Expiry / **Created by** (falling back to "System" for keys with no recorded creator, e.g. auto-generated). Also shows a **Tags** column (system-set key-value tags, displayed read-only via an `(i)` tooltip). Per-component parameters: `ShowScopeTooltip` (effective-scope `(i)`, default on), `ShowScopeOverrides` (scope-override editor), `ShowRoles` (tenant-role editor), `ShowLastUsed` (Last used column; 60-day expiry warning), `ShowExpiryDatePicker`, `ShowTags` (`bool?` — null = auto-show when any key has tags), `ChipTagKeys`, `ShowAuditLogButton`, `AllowGridSorting` (sort by Name / Last used, default on, Name ascending), `AllowGridFiltering` (case-insensitive Name text filter, default off), `ShowPrivateKeys` (`None`/`Mine`/`All` — include owner-scoped "private" keys; default None) and `AllowPrivilegedAccess` (let Administrator/Owner *see* private keys when `All`; view-only). `TeamComponent` shares `ShowScopeTooltip`/`ShowScopeOverrides`/`ShowRoles`; `SystemApiKeyView` uses global **system scopes** (`o.ConfigureSystemScopes`). Access is gated on the `apikey:manage` scope; cross-team access comes from mapping a role to system scopes (`o.ConfigureSystemRoles`), not per-component role parameters. "Last used" writes are throttled by `ApiKeyOptions.LastUsedThrottle` (default 1 min).
- **Scope explorer** - `ScopeView` shows which scopes a member would have: pick an **access level** (single-select bar; Owner/Administrator are merged since they grant the same scopes) and **roles** (multi-select bar), and scopes not granted by the selection are **greyed out**. Defaults to the signed-in member's own access level, roles, and **scope overrides** (overrides are highlighted with a ⭐ and an `Override` badge). Built dynamically from `IScopeRegistry` / `ITenantRoleRegistry`, so it always reflects the live configuration (no hard-coded list). Parameters: `ShowDescription` (default on), `ShowAccessLevelSelector` (default on), `ShowRoles` (default on; the roles bar auto-hides when no tenant roles are configured), `AllowGridSorting` (sort by scope name, default on), `AllowGridFiltering` (case-insensitive name filter, default off). Shows a friendly notice when no scopes are configured.
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

## Dependencies

- [Tharga.Blazor](https://www.nuget.org/packages/Tharga.Blazor) - Generic UI components.
- [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) - Domain models and authorization primitives.
- [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) - Audit types for AuditLogView.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams and users |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Server-side API key auth, Swagger, audit logging |

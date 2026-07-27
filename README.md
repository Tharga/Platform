# Tharga Team

A suite of NuGet packages for building multi-tenant Blazor applications with team management, authorization, and API infrastructure.

## Packages

| Package | Description | WASM-safe |
|---------|-------------|-----------|
| [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) | Domain models, authorization primitives, service abstractions | Yes |
| [Tharga.Blazor](https://www.nuget.org/packages/Tharga.Blazor) | Generic Blazor UI components (buttons, breadcrumbs, etc.) | Yes |
| [Tharga.Team.Blazor](https://www.nuget.org/packages/Tharga.Team.Blazor) | Team management Blazor components | Yes |
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams and users | No |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Server-side API key auth, Swagger, audit logging | No |
| [Tharga.Team.Entra](https://www.nuget.org/packages/Tharga.Team.Entra) | Microsoft Entra ID user directory (verify / list / delete users via Graph) | No |
| [Tharga.Team.Images](https://www.nuget.org/packages/Tharga.Team.Images) | Automatic downscaling of uploaded team/user icons (ImageSharp) | No |
| [Tharga.Team.Mcp](https://www.nuget.org/packages/Tharga.Team.Mcp) | MCP (Model Context Protocol) bridge — auth, scopes, audit for MCP tools. Renamed from `Tharga.Platform.Mcp`, which is deprecated and frozen at 3.5.x | No |

## Dependency graph

```
Tharga.Team ── plain .NET, no external dependencies
├── Tharga.Blazor ── generic Blazor UI components
│   └── Tharga.Team.Blazor ── team management UI
│       └── + Tharga.Team.Service
├── Tharga.Team.MongoDB ── persistence layer
│   └── + Tharga.MongoDB
├── Tharga.Team.Service ── server-side API + auth
│   └── + Tharga.MongoDB, ASP.NET Core
├── Tharga.Team.Entra ── Entra ID user directory (optional)
│   └── + Azure.Identity, Microsoft Graph REST
└── Tharga.Team.Images ── icon downscaling (optional)
    └── + SixLabors.ImageSharp
```

## Quick Start

Install the packages:

```
dotnet add package Tharga.Team.Blazor
dotnet add package Tharga.Team.Service
dotnet add package Tharga.Team.MongoDB
```

Register everything in `Program.cs`:

```csharp
// One call to set up auth, Blazor, controllers, API keys
builder.AddThargaTeam(o =>
{
    o.Blazor.Title = "My App";
    o.Blazor.RegisterTeamService<MyTeamService, MyUserService>();
});

// MongoDB persistence (requires consumer-specific entity types)
builder.Services.AddMongoDB(o => { /* connection config */ });
builder.Services.AddThargaTeamRepository(o =>
{
    o.UseUserEntity<MyUserEntity>();
    o.UseTeamEntity<MyTeamEntity, MyTeamMember>();
});

var app = builder.Build();

app.UseThargaTeam();
```

Add to `appsettings.json`:

```json
{
  "AzureAd": {
    "Authority": "https://your-tenant.ciamlogin.com/your-tenant-id",
    "ClientId": "your-client-id",
    "TenantId": "your-tenant-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

Optional features (pass via `ThargaTeamOptions`):

```csharp
builder.AddThargaTeam(o =>
{
    // Fine-grained scopes
    o.ConfigureScopes = scopes =>
    {
        scopes.Register("orders:read", AccessLevel.Viewer);
        scopes.Register("orders:write", AccessLevel.Administrator);
    };

    // Named roles that bundle scopes
    o.ConfigureTenantRoles = roles =>
    {
        roles.Register("Editor", new[] { "orders:read", "orders:write" });
    };

    // Optional: let team admins define their own custom roles at runtime (assignable to members and
    // API keys via <TenantRoleManager /> and <ApiKeyView ShowRoles="true" />).
    // o.EnableDynamicRoles = true;
    // o.DynamicRoleManageScope = "access:manage"; // scope for custom-role CRUD (default team:manage)

    // Audit logging (StorageMode defaults to Logger only — set MongoDB to populate AuditLogView)
    o.Audit = new AuditOptions { StorageMode = AuditStorageMode.MongoDB };

    // Capture an API key's private token on create/recycle (e.g. to re-deliver a minted key)
    o.AddApiKeyLifecycleHandler<MyApiKeyHandler>();
});
```

API-key behaviour (auto-lock, expiry, and the random secret length via `MinKeyLength`/`MaxKeyLength`) is configured on `o.ApiKey`. See the [Tharga.Team.Service README](Tharga.Team.Service/README.md#api-key-options) and the [Implementation Guide](docs/articles/implementation-guide.md).

## User administration & Entra directory

The user store tracks per-user **last seen** (opt-in: declare `LastSeen`/`DirectoryId` on your user entity), and `IUserManagementService` provides audited administration: verify users against Microsoft Entra ID, list users that exist only in Entra, and delete users — from the app and (explicit opt-in) from Entra. Everything cross-user — including viewing the admin lists and enumerating users via `IUserService` — requires the `users:manage` system scope, enforced in the service layer:

```csharp
// dotnet add package Tharga.Team.Entra
builder.Services.AddThargaEntraUserDirectory(builder.Configuration);   // AzureAd section + ClientSecret

builder.AddThargaTeam(o =>
{
    o.ConfigureSystemRoles = roles => roles.Map("Developer", SystemUserScopes.Manage);
});
```

The `<UsersView />` admin component picks it all up automatically. See [User management & directory](docs/articles/user-management.md).

## Team & user icons

Teams and users get real icons/avatars via two pluggable seams — **storage** (`IIconStore`, default MongoDB, no extra package) and **sourcing** (`IIconSource`: stored icon → custom → Gravatar → default → initials). Team icons need no entity change; add `Icon` to your user entity to enable user icons. A `team:manage` holder sets a team icon (upload or URL) from the team component; users upload their own from the profile page (an alternative to Gravatar), and admins (`users:manage`) can set a user's icon. Behavior is configurable and runtime-adjustable via `o.IconSettings` (Gravatar on/off + style, a default image, upload toggles). Add the optional `Tharga.Team.Images` package to auto-downscale oversized uploads (256 px) instead of rejecting them:

```csharp
builder.Services.AddThargaImageProcessing();   // optional: auto-downscale via ImageSharp
```

See [Team & user icons](docs/articles/icons.md).

## Live claim revalidation

Team claims (membership, access level, tenant-role scopes, consent access) are enriched at HTTP authentication, so in a long-lived Blazor Server circuit they would otherwise stay frozen until a reload — a removed member, a downgraded access level, or a revoked consent would keep their old access, in the service-layer checks as well as the UI. Tharga.Team revalidates them on an interval and refreshes the principal **in place** (no forced sign-out), so team access is stale for at most one interval. On by default (30 min); tune or disable it:

```csharp
builder.AddThargaTeam(o =>
{
    o.Blazor.ClaimRevalidation.Interval = TimeSpan.FromMinutes(5); // narrow the window
    // o.Blazor.ClaimRevalidation.Enabled = false;                 // or turn it off
});
```

See [Team-claim revalidation](docs/articles/implementation-guide.md#team-claim-revalidation).

## Advanced Usage

Individual `Add*` methods remain available for partial/custom setups. See the **[Implementation Guide](docs/articles/implementation-guide.md)** for step-by-step instructions.

## Links

- [Implementation Guide](docs/articles/implementation-guide.md)
- [Documentation site](https://team.tharga.net)
- [Report an issue](https://github.com/Tharga/Team/issues)

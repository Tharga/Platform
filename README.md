# Tharga Platform

A suite of NuGet packages for building multi-tenant Blazor applications with team management, authorization, and API infrastructure.

## Packages

| Package | Description | WASM-safe |
|---------|-------------|-----------|
| [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) | Domain models, authorization primitives, service abstractions | Yes |
| [Tharga.Blazor](https://www.nuget.org/packages/Tharga.Blazor) | Generic Blazor UI components (buttons, breadcrumbs, etc.) | Yes |
| [Tharga.Team.Blazor](https://www.nuget.org/packages/Tharga.Team.Blazor) | Team management Blazor components | Yes |
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams and users | No |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Server-side API key auth, Swagger, audit logging | No |

## Dependency graph

```
Tharga.Team ── plain .NET, no external dependencies
├── Tharga.Blazor ── generic Blazor UI components
│   └── Tharga.Team.Blazor ── team management UI
│       └── + Tharga.Team.Service
├── Tharga.Team.MongoDB ── persistence layer
│   └── + Tharga.MongoDB
└── Tharga.Team.Service ── server-side API + auth
    └── + Tharga.MongoDB, ASP.NET Core
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
builder.AddThargaPlatform(o =>
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

app.UseThargaPlatform();
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

Optional features (pass via `ThargaPlatformOptions`):

```csharp
builder.AddThargaPlatform(o =>
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

    // Audit logging
    o.Audit = new AuditOptions { StorageMode = AuditStorageMode.MongoDB };
});
```

## Advanced Usage

Individual `Add*` methods remain available for partial/custom setups. See the **[Implementation Guide](docs/implementation-guide.md)** for step-by-step instructions.

## Links

- [Implementation Guide](docs/implementation-guide.md)
- [Report an issue](https://github.com/Tharga/Platform/issues)

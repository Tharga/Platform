# Tharga Team Blazor
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Blazor)](https://www.nuget.org/packages/Tharga.Team.Blazor)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Blazor)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Team management Blazor components for multi-tenant applications. Works with both **Blazor Server** and **Blazor WebAssembly**.

## Components

- **Team management** - `TeamSelector`, `TeamComponent`, `TeamDialog`, `InviteUserDialog`, `TeamInviteView`.
- **API key management** - `ApiKeyView` for team-scoped API keys.
- **User management** - `UserProfileView`, `UsersView`.
- **Authentication** - `LoginDisplay` with login/logout and team navigation.
- **Claims augmentation** - `TeamClaimsAuthenticationStateProvider` adds `TeamKey`, `AccessLevel`, role, and scope claims. Compatible with all hosting models.
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

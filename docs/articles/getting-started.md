# Getting started

This page gets you from nothing to a working multi-tenant app with team management, API-key authentication, and the Blazor UI components. For the full breakdown of every option — and a step-by-step alternative to the single call below — see the [Implementation guide](implementation-guide.md).

## Install

```
dotnet add package Tharga.Team.Blazor
dotnet add package Tharga.Team.Service
dotnet add package Tharga.Team.MongoDB
```

- **Tharga.Team.Blazor** — the UI components and authentication.
- **Tharga.Team.Service** — server-side API-key auth, Swagger, audit.
- **Tharga.Team.MongoDB** — persistence. Swap this for your own `ITeamService` / `IUserService` backend if you don't use MongoDB.

## Register

```csharp
builder.AddThargaTeam(o =>
{
    o.Blazor.Title = "My App";
    o.Blazor.RegisterTeamService<MyTeamService, MyUserService>();
});

var app = builder.Build();
app.UseThargaTeam();
```

`AddThargaTeam` registers, with sensible defaults:

| Concern | What you get |
|---|---|
| Authentication | Azure AD + OIDC login/logout endpoints and claims augmentation. |
| API keys | Header-based API-key authentication and management/administration services. |
| Blazor UI | `TeamSelector`, `TeamComponent`, `ApiKeyView`, `SystemApiKeyView`, `UsersView`, `LoginDisplay`, `AuditLogView`, and more. |
| Controllers | API controllers + Swagger integration. |

## Configure authentication

`AddThargaTeam` expects an `AzureAd` section in `appsettings.json`:

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

## Implement the backend types

`RegisterTeamService<MyTeamService, MyUserService>()` needs your two service implementations. With the MongoDB package these derive from the provided base classes and entity types — see [Step 4: Team Management](implementation-guide.md#step-4-team-management) for the entities, `UserService`, and `TeamService`.

**Or write none of them.** `Tharga.Team.MongoDB` ships a standard set, so the smallest working registration is:

```csharp
o.Blazor.RegisterTeamService<DefaultTeamService, DefaultUserService>();
```

```csharp
builder.Services.AddThargaTeamRepository(o =>
{
    o.RegisterUserRepository<DefaultUserEntity>();
    o.RegisterTeamRepository<DefaultTeamEntity, DefaultTeamMember>();
});
```

Swap in your own type at any point: they are ordinary classes, and every member is virtual. Reach for
your own when a team, member or user needs properties of your own — see
[Using your own entities](implementation-guide.md#using-your-own-entities).

## Next

- [Implementation guide](implementation-guide.md) — the full setup, option by option
- [Scopes](implementation-guide.md#step-6-scopes) — register scopes and gate methods with `[RequireScope]`
- [Tenant roles](implementation-guide.md#step-7-tenant-roles) and [managing roles & scopes](implementation-guide.md#step-7b-managing-roles--scopes-reference) — roles, system scopes, and consent
- [Audit logging](implementation-guide.md#step-8-audit-logging) — record and view authorization-relevant events

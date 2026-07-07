---
_layout: landing
---

# Tharga.Platform

Multi-tenant **team, user, and API-key management** for Blazor applications. Built for **.NET 8 / 9 / 10**. Provides teams with members and invitations, an access-level + scope + tenant-role authorization model, system API keys, audit logging, and a set of ready-made Blazor UI components — all backed by pluggable persistence (MongoDB out of the box).

## Packages

| Package | What it does |
|---|---|
| [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) | Domain models, service abstractions, and authorization primitives. No server-side dependencies — works in Blazor Server and WebAssembly. |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Server-side API-key authentication, Swagger integration, and audit logging. |
| [Tharga.Team.Blazor](https://www.nuget.org/packages/Tharga.Team.Blazor) | Team / user / API-key management UI components and authentication display. |
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams, users, and API keys. |

## Quick start

```
dotnet add package Tharga.Team.Blazor
dotnet add package Tharga.Team.Service
dotnet add package Tharga.Team.MongoDB
```

```csharp
builder.AddThargaPlatform(o =>
{
    o.Blazor.Title = "My App";
    o.Blazor.RegisterTeamService<MyTeamService, MyUserService>();
});

var app = builder.Build();
app.UseThargaPlatform();
```

`AddThargaPlatform` wires up authentication (Azure AD + OIDC), API-key authentication, the Blazor components, and the controllers with sensible defaults. See the [Implementation guide](articles/implementation-guide.md) for the full setup, including MongoDB and the step-by-step alternative.

## Authorization model at a glance

- **`AccessLevel`** — Owner, Administrator, User, Viewer, and **Custom** (no inherited base scopes, for least-privilege keys/members).
- **Scopes** — register team scopes per access level (`IScopeRegistry`) and gate service methods with `[RequireScope]`.
- **Tenant roles** — bundle scopes into named roles a team can assign to members and API keys. Roles can be code-registered (`ITenantRoleRegistry`) or, with `o.EnableDynamicRoles`, defined by team admins at runtime as custom per-team roles (`ITenantRoleService`, managed via `<TenantRoleManager />`); the scope required to manage custom roles is configurable (`o.DynamicRoleManageScope`, default `team:manage`).
- **System scopes & roles** — global scopes for system API keys, plus an app-role → system-scope mapping for privileged users (`o.ConfigureSystemScopes` / `o.ConfigureSystemRoles`).
- **Consent** — a team grants global roles cross-team access at a chosen access level.

See [Roles & scopes](articles/implementation-guide.md#step-7b-managing-roles--scopes-reference) for the full reference.

## Where next

- **[Articles](articles/index.md)** — getting started and the full implementation guide
- **[API reference](xref:Tharga.Team)** — every public type, method, and option, generated from XML doc comments
- **[GitHub](https://github.com/Tharga/Platform)** — source, issues, releases

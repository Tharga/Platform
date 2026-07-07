# Tharga Team
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team)](https://www.nuget.org/packages/Tharga.Team)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Domain models, service abstractions, and authorization primitives for multi-tenant Blazor applications. This package has **no server-side dependencies** and works with both Blazor Server and Blazor WebAssembly.

## What's included

### Team and user models
- `ITeam` / `ITeam<TMember>` - Team aggregate with members.
- `ITeamMember` - Team member with `AccessLevel`, invitation state, tenant roles, and scope overrides.
- `IUser` - User identity.
- `Invitation`, `InviteUserModel`, `MembershipState`.

### Service interfaces
- `ITeamService` - Team CRUD, member management, invitations. Includes `GetMembersAsync(teamKey)` returning `IAsyncEnumerable<ITeamMember>` for consumers that need to enumerate members without knowing the per-consumer `TMember` type.
- `ITeamManagementService` - Scope-enforced mutations (create, rename, delete, invite, etc.).
- `IUserService` - Current user resolution.
- `IApiKeyAdministrationService` / `IApiKeyManagementService` - API key management.
- `IApiKeyLifecycleHandler` - Opt-in hook receiving an API key's private token on create/recycle (and a tokenless delete signal) via `ApiKeyLifecycleContext` / `ApiKeyLifecycleReason`. Registered through `ThargaPlatformOptions.AddApiKeyLifecycleHandler<T>()`.

### Authorization
- `AccessLevel` enum - Owner, Administrator, User, Viewer, Custom. `Custom` grants no inherited base scopes (effective scopes = roles ∪ scope overrides only) for least-privilege keys/members.
- `Tag` record - System-set key-value tag on an API key (a list, so a key may repeat). Set at creation only; surfaced as a `tag.{Key}` claim on the authenticated principal.
- `TeamClaimTypes` - Claim type constants (`TeamKey`, `AccessLevel`, `Scope`, `TagPrefix`).
- `IScopeRegistry` / `ScopeRegistry` - Register and resolve scopes per access level.
- `ITenantRoleRegistry` / `TenantRoleRegistry` - Register **code roles** (global, fixed at deploy time) with associated scopes.
- `ITenantRoleService` / `TenantRoleService` - Team-aware role resolution: merges code roles with a team's **runtime-defined custom roles** and unions their scopes for a member of a given team. Registered by `AddThargaDynamicTenantRoles()` (wired when `o.EnableDynamicRoles = true`); custom roles are created/edited per team via `ITeamService.SetTeamCustomRolesAsync`, with scopes constrained to app-registered scopes. The scope required to manage custom roles is configurable via `AddThargaDynamicTenantRoles(o => o.ManageScope = "…")` (default `team:manage`).
- `ITenantRoleVisibilityProvider` - Optional per-team hook that decides whether a tenant role is offered in the role editor. Default (`AllRolesVisibleTenantRoleVisibilityProvider`) shows every role; register your own to hide feature-gated roles from teams where the feature is disabled. Hiding a role never prunes existing assignments and does not affect scope resolution.
- `RequireAccessLevelAttribute` / `RequireScopeAttribute` - Declarative authorization on service methods.
- `TeamScopes` / `ApiKeyScopes` / `AuditScopes` - Built-in scope constants (`audit:read` gates the audit log).
- `ISystemScopeRegistry` / `ISystemRoleRegistry` - Global (system) scopes for system API keys, and a mapping of app/global roles (e.g. `Developer`) to those scopes for privileged users. Configured via `o.ConfigureSystemScopes` / `o.ConfigureSystemRoles`.

### Base classes
- `TeamServiceBase` - Implement your own team service backend.
- `UserServiceBase` - Implement your own user service backend.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Team.Blazor](https://www.nuget.org/packages/Tharga.Team.Blazor) | Team management Blazor UI components, authentication |
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams and users |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Server-side API key auth, Swagger, audit logging |
| [Tharga.Blazor](https://www.nuget.org/packages/Tharga.Blazor) | Generic Blazor UI components (buttons, breadcrumbs, etc.) |

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

### Authorization
- `AccessLevel` enum - Owner, Administrator, User, Viewer.
- `TeamClaimTypes` - Claim type constants (`TeamKey`, `AccessLevel`, `Scope`).
- `IScopeRegistry` / `ScopeRegistry` - Register and resolve scopes per access level.
- `ITenantRoleRegistry` / `TenantRoleRegistry` - Register tenant roles with associated scopes.
- `RequireAccessLevelAttribute` / `RequireScopeAttribute` - Declarative authorization on service methods.
- `TeamScopes` / `ApiKeyScopes` - Built-in scope constants.

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

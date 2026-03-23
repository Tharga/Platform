# Feature Request: Simplify Team Registration & Fix Missing Service Registrations

**Requested by:** Daniel Bohlin
**Date:** 2026-03-23
**Found in:** Tharga.Team.* 2.0.1-pre.3
**Context:** Discovered while integrating Team Management into Eplicta.FortDocs.Web

## Problem

Setting up Tharga Team Management requires too many manual steps and undocumented workarounds. A consumer currently needs 7+ separate registration calls, and several required services are not auto-registered by the methods that expose the components depending on them.

## Current Consumer Code Required

```csharp
// 1. Team Blazor (registers ITeamService, IUserService, TeamStateService)
builder.Services.AddThargaTeamBlazor(o =>
{
    o.RegisterTeamService<MyTeamService, MyUserService>();
});

// 2. Workaround: ITeamManagementService not registered by AddThargaTeamBlazor
//    but TeamComponent requires it via [Inject]
builder.Services.AddScoped<ITeamManagementService, TeamManagementService<MyMember>>();

// 3. Workaround: IApiKeyManagementService not registered by AddThargaApiKeys
//    but ApiKeyView requires it via [Inject]
builder.Services.AddScoped<IApiKeyManagementService, ApiKeyManagementService>();

// 4. Scopes must be manually registered or TeamComponent hides all management UI
builder.Services.AddThargaScopes(scopes =>
{
    scopes.Register(TeamScopes.Read, AccessLevel.Viewer);
    scopes.Register(TeamScopes.Manage, AccessLevel.Administrator);
    scopes.Register(TeamScopes.MemberInvite, AccessLevel.Administrator);
    scopes.Register(TeamScopes.MemberRemove, AccessLevel.Administrator);
    scopes.Register(TeamScopes.MemberRole, AccessLevel.Administrator);
});

// 5. MongoDB repositories
builder.Services.AddThargaTeamRepository(o =>
{
    o.RegisterTeamEntity<MyTeamEntity, MyMemberModel>();
    o.RegisterUserEntity<MyUserEntity>();
});

// 6. Workaround: MongoDB assembly scanning misses Tharga.Team.Service
//    because entry assembly name prefix doesn't match "Tharga"
builder.AddMongoDB(o =>
{
    o.AddAutoRegistrationAssembly(typeof(ApiKeyConstants).Assembly);
});

// 7. API key authentication (if needed)
builder.Services.AddThargaApiKeyAuthentication<MyApiKeyService>();
```

## Requested Improvements

### 1. AddThargaTeamBlazor should register ITeamManagementService

When `RegisterTeamService<TTeam, TUser>()` is called, the `TMember` type is known. `AddThargaTeamBlazor` should also register:
```csharp
services.AddScoped<ITeamManagementService, TeamManagementService<TMember>>();
```

**Why:** `TeamComponent` has `[Inject] ITeamManagementService`. If the method that exposes `TeamComponent` doesn't register its dependency, the page crashes with an opaque `InvalidOperationException`.

### 2. AddThargaApiKeys should register IApiKeyManagementService

```csharp
services.AddScoped<IApiKeyManagementService, ApiKeyManagementService>();
```

**Why:** Same pattern — `ApiKeyView` requires `IApiKeyManagementService` but only `IApiKeyAdministrationService` is registered.

### 3. Register default TeamScopes when no scopes are configured

When `AddThargaScopes()` is not called (or called without arguments), auto-register sensible defaults:
```csharp
TeamScopes.Read       → AccessLevel.Viewer
TeamScopes.Manage     → AccessLevel.Administrator
TeamScopes.MemberInvite → AccessLevel.Administrator
TeamScopes.MemberRemove → AccessLevel.Administrator
TeamScopes.MemberRole   → AccessLevel.Administrator
ApiKeyScopes.Manage     → AccessLevel.Administrator
```

**Why:** Without scopes, `TeamComponent` hides all management buttons even for team owners. The current behavior (silently hiding everything) gives no indication of misconfiguration.

### 4. Platform packages should register their own assemblies for MongoDB scanning

`AddThargaTeamRepository()` or `AddThargaApiKeys()` should call `AddAutoRegistrationAssembly` for `Tharga.Team.Service` internally, so consumers don't need to know about assembly scanning internals.

**Why:** The entry assembly name prefix scan (`Eplicta.FortDocs` doesn't match `Tharga.Team`) silently fails to register `IApiKeyRepository`, causing an `AggregateException` at startup with no hint about the root cause.

### 5. Provide AddThargaPlatform() single entry point

A single method that sets up all core services with sensible defaults:
```csharp
builder.AddThargaPlatform(o =>
{
    o.Title = "My App";
    o.RegisterTeamService<MyTeamService, MyUserService>();
    o.RegisterTeamEntity<MyTeamEntity, MyMemberModel>();
    o.RegisterUserEntity<MyUserEntity>();
});
```

This should internally call `AddThargaTeamBlazor`, `AddThargaTeamRepository`, `AddThargaScopes` (with defaults), register `ITeamManagementService`, `IApiKeyManagementService`, and handle assembly scanning.

Individual `Add*` methods remain available for advanced/partial use.

### 6. Graceful degradation with clear error messages

When a required service is missing, components should render a clear in-page error message instead of crashing:
```
"Team management is not configured. Call AddThargaTeamBlazor() in Program.cs."
```

This pattern is already partially implemented for `ApiKeyView` (which shows a message when `IApiKeyManagementService` is missing) but not for `TeamComponent` or other components.

## Impact

These issues affect every project adopting Tharga.Team: FortDocs.Web, Quilt4Net Server, PlutusWave, and any future consumers. Each project currently has to independently discover the correct combination of registration calls through trial and error.

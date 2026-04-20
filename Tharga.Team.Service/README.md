# Tharga Team Service
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Service)](https://www.nuget.org/packages/Tharga.Team.Service)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Service)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Server-side API-key authentication, authorization enforcement, controller registration, OpenAPI/Swagger setup, and audit logging for ASP.NET Core projects. Targets .NET 9.0 and .NET 10.0.

## Features

- **API key authentication** - Reads the `X-API-KEY` header, validates against a store, and populates `TeamKey`, `AccessLevel`, and scope claims.
- **Access level authorization** - `AccessLevelProxy<T>` enforces `[RequireAccessLevel]` on service methods via `DispatchProxy`.
- **Scope authorization** - `ScopeProxy<T>` enforces `[RequireScope]` with audit logging.
- **Controller + Swagger registration** - Single-call setup for MVC controllers, OpenAPI document with API key security scheme, and Swagger UI.
- **API key management** - Default MongoDB-backed `ApiKeyAdministrationService` with key hashing.
- **Audit logging** - `CompositeAuditLogger` with ILogger and MongoDB storage backends.
- **Pluggable** - Implement `IApiKeyAdministrationService` (from Tharga.Team) to bring your own storage backend.

## Quick start

```csharp
using Tharga.Team;
using Tharga.Team.Service;

// Program.cs
builder.Services.AddThargaControllers();
builder.Services.AddAuthentication()
    .AddThargaApiKeyAuthentication();
builder.Services.AddThargaApiKeys();

var app = builder.Build();
app.UseThargaControllers();
app.UseAuthentication();
app.UseAuthorization();
app.Run();
```

## System API keys

For infrastructure-level credentials that aren't tied to a team (MCP gatekeepers, CI/CD callers, cross-team admin tooling), use **system keys** — API keys with no `TeamKey`.

Create and manage them via the `<SystemApiKeyView />` component in `Tharga.Team.Blazor` (gated by the `Developer` role), or programmatically via `IApiKeyAdministrationService.CreateSystemKeyAsync(name, scopes, expiryDate, createdBy)`.

System keys authenticate through the same `X-API-KEY` header. The principal they produce carries the `IsSystemKey=true` claim and the explicit scopes granted at creation time — no `TeamKey` claim.

Protect system-only endpoints with the system policy:

```csharp
app.MapMcp().RequireAuthorization(ApiKeyConstants.SystemPolicyName);
```

The two policies are mutually exclusive: `ApiKeyPolicy` rejects system keys, `SystemApiKeyPolicy` rejects team keys.

## Team API keys

Protect endpoints with the built-in policy:

```csharp
[Authorize(Policy = ApiKeyConstants.PolicyName)]
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var teamKey = User.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        return Ok(new { teamKey });
    }
}
```

Enforce access levels on services:

```csharp
public interface IMyService
{
    [RequireAccessLevel(AccessLevel.Viewer)]
    IAsyncEnumerable<Item> GetAsync();

    [RequireAccessLevel(AccessLevel.User)]
    Task<Item> AddAsync(string name);
}

// Program.cs
builder.Services.AddScopedWithAccessLevel<IMyService, MyService>();
```

## Dependencies

- [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) - Domain models, authorization primitives, and service abstractions.
- [Tharga.MongoDB](https://www.nuget.org/packages/Tharga.MongoDB) - MongoDB repository infrastructure.
- [Tharga.Toolkit](https://www.nuget.org/packages/Tharga.Toolkit) - Shared utilities including API key hashing.
- [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore) - Swagger UI generation.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Team](https://www.nuget.org/packages/Tharga.Team) | Domain models and authorization primitives (plain .NET, WASM-safe) |
| [Tharga.Team.Blazor](https://www.nuget.org/packages/Tharga.Team.Blazor) | Team-specific Blazor UI components |
| [Tharga.Blazor](https://www.nuget.org/packages/Tharga.Blazor) | Generic Blazor UI components |
| [Tharga.Team.MongoDB](https://www.nuget.org/packages/Tharga.Team.MongoDB) | MongoDB persistence for teams and users |

## Links

- [GitHub repository](https://github.com/Tharga/Platform)
- [Report an issue](https://github.com/Tharga/Platform/issues)

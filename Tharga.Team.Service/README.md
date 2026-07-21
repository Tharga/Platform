# Tharga Team Service
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Service)](https://www.nuget.org/packages/Tharga.Team.Service)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Service)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Server-side API-key authentication, authorization enforcement, controller registration, OpenAPI/Swagger setup, and audit logging for ASP.NET Core projects. Targets .NET 9.0 and .NET 10.0.

## Features

- **API key authentication** - Reads the `X-API-KEY` header, validates against a store, and populates `TeamKey`, `AccessLevel`, and scope claims.
- **Access level authorization** - `AccessLevelProxy<T>` enforces `[RequireAccessLevel]` on service methods via `DispatchProxy`.
- **Scope authorization** - `ScopeProxy<T>` enforces `[RequireScope]` with audit logging.
- **Works in API and interactive Blazor** - the proxies resolve the caller via `ITeamPrincipalAccessor` (default: `IHttpContextAccessor`). `AddThargaTeamBlazor` swaps in a circuit-aware accessor (HttpContext when present, else `AuthenticationStateProvider`), so one `[RequireScope]`/`[RequireAccessLevel]` enforces both surfaces. Register a custom `ITeamPrincipalAccessor` to plug in another principal source.
- **Controller + Swagger registration** - Single-call setup for MVC controllers, OpenAPI document with API key security scheme, and Swagger UI.
- **API key management** - Default MongoDB-backed `ApiKeyAdministrationService` with key hashing. Configurable via `ApiKeyOptions` — see [API key options](#api-key-options).
- **Audit logging** - `CompositeAuditLogger` with `ILogger` and MongoDB backends. ⚠️ Stores to `ILogger` only by default — see [Audit logging](#audit-logging).
- **API-key lifecycle hook** - Capture the private token on create/recycle (plus a delete signal) via `IApiKeyLifecycleHandler` — see [Capturing the private token](#capturing-the-private-token).
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

## Customizing the OpenAPI document

`AddThargaControllers` owns the OpenAPI document (it registers the API-key security scheme on it). To add your own `IOpenApiDocumentTransformer` / `IOpenApiOperationTransformer` — for example, to filter the generated spec down to the operations the current caller is authorized for — use the `ConfigureOpenApi` hook instead of calling `AddOpenApi("v1", …)` yourself:

```csharp
builder.Services.AddThargaControllers(o =>
    o.ConfigureOpenApi(api => api.AddDocumentTransformer<ScopeFilteringDocumentTransformer>()));
```

The callback receives the same `OpenApiOptions` Tharga configures, so your transformers run against the document Tharga already manages. Multiple `ConfigureOpenApi` calls compose (each runs, in registration order). This avoids a second `AddOpenApi("v1", …)` registration — which would leave it ambiguous whether your document composes with or overrides Tharga's, and, in .NET 10, forces the OpenAPI XML-comment source generator to emit an interceptor into your project (requiring `<InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>` just to compile).

> **.NET 10+ only.** On .NET 9 the document is built by Swashbuckle and this hook is not present; use Swashbuckle's `IDocumentFilter` / `IOperationFilter` there.

## System API keys

For infrastructure-level credentials that aren't tied to a team (MCP gatekeepers, CI/CD callers, cross-team admin tooling), use **system keys** — API keys with no `TeamKey`.

Create and manage them via the `<SystemApiKeyView />` component in `Tharga.Team.Blazor` (gated by the `Developer` role), or programmatically via `IApiKeyAdministrationService.CreateSystemKeyAsync(name, scopes, expiryDate, createdBy)`.

System keys authenticate through the same `X-API-KEY` header. The principal they produce carries the `IsSystemKey=true` claim and the explicit scopes granted at creation time — no `TeamKey` claim.

Protect system-only endpoints with the system policy:

```csharp
app.UseThargaMcp().RequireAuthorization(ApiKeyConstants.SystemPolicyName);
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

## API key options

API key behaviour is configured via `ApiKeyOptions` (passed to `AddThargaApiKeyAuthentication`, or `o.ApiKey` under `AddThargaPlatform`):

| Option | Default | Purpose |
|---|---|---|
| `AdvancedMode` | `false` | When false, keys are auto-created per team and only refresh/lock are exposed. When true, full CRUD (name, access level, roles, scope overrides, expiry). |
| `AutoKeyCount` | `2` | Number of keys auto-created per team in simple mode. |
| `AutoLockKeys` | `false` | Lock keys immediately after creation so the raw value is shown only once. |
| `MaxExpiryDays` | `365` | Caps expiry for team and system keys. `null` = no cap. |
| `LastUsedThrottle` | `1 min` | Minimum interval between `LastUsedAt` writes for a key (avoids a DB write per request). `TimeSpan.Zero` = stamp every request. |
| `MinKeyLength` / `MaxKeyLength` | `24` / `32` | Random alphanumeric length of the key secret (base62, ≈5.95 bits/char). The length is chosen at random in `[Min, Max]` per key. ~190-bit at the default 32; 43 ≈ 256-bit. Floor 24 (≈143-bit). |

## Audit logging

`AddThargaAuditLogging` records mutations (team-service operations **and** API-key management) and authorization events via `CompositeAuditLogger`.

```csharp
builder.Services.AddThargaAuditLogging(o =>
{
    o.StorageMode = AuditStorageMode.MongoDB;   // see gotcha below
    o.RetentionDays = 90;                       // null (or <= 0) = keep forever
});
```

> **⚠️ Gotcha:** `StorageMode` defaults to **`Logger` only**, so the MongoDB-backed `AuditLogView` stays **empty** until you set `AuditStorageMode.MongoDB` (or `Logger | MongoDB`). `AuditStorageMode` is a `[Flags]` enum.

| Option | Default | Notes |
|---|---|---|
| `StorageMode` | `Logger` | `[Flags]`: `Logger`, `MongoDB`, or both. Set `MongoDB` to populate `AuditLogView`. |
| `CallerFilter` / `EventFilter` | `Api\|Web` / `All` | `[Flags]` — which caller sources / event types to record. |
| `ExcludedActions` / `ExcludedEndpoints` | empty | Skip noisy actions (e.g. `"read"`) or endpoints. |
| `RetentionDays` | `90` | `int?` → MongoDB TTL index (`Timestamp_TTL`). **`null` or `<= 0` = keep forever** (no TTL index). Changing/removing the TTL on an existing collection may need a manual index drop. |
| `BatchSize` / `FlushIntervalSeconds` | `100` / `5` | Background MongoDB writer tuning. |

### Operation metadata

Audited operations record **what changed** on `AuditEntry.Metadata` — create captures the team name,
rename the old and new name, a role change the old and new access level, consent the old and new level and
roles, and so on (keys are defined on `AuditMetadataKeys`). Capturing a "before" value is best-effort and
never fails the operation. Metadata is shown as an expandable row in `AuditLogView`, in CSV export (a
JSON-encoded `Metadata` column), JSON export, and the `Logger` output.

### Adding your own metadata

Register an `IAuditEnricher` to attach host-defined metadata to every entry the toolkit writes:

```csharp
public sealed class RequestIdAuditEnricher(IHttpContextAccessor http) : IAuditEnricher
{
    public void Enrich(AuditEntry entry, IDictionary<string, string> metadata)
    {
        if (http.HttpContext?.TraceIdentifier is { } id) metadata["request.id"] = id;
    }
}

builder.Services.AddThargaAuditEnricher<RequestIdAuditEnricher>();
```

Enrichers run in registration order for every entry that passes the filters. The merge is **add-only** —
an enricher cannot overwrite a key the toolkit (or an earlier enricher) set — is resolved as a
**singleton** (read request state via `IHttpContextAccessor`), and one that throws is logged and skipped so
enrichment can never fail the audited operation.

See the [implementation guide](https://platform.tharga.net) for the full reference.

## Capturing the private token

The private token is shown once and never persisted, logged, or exposed over an API. To capture it (e.g. to re-deliver a minted key), register an `IApiKeyLifecycleHandler` — it receives the token on **create** and **recycle/regenerate**, plus a tokenless **delete** signal:

```csharp
public class MyHandler(ISecretProtector protector, IMyStore store) : IApiKeyLifecycleHandler
{
    public Task OnApiKeyLifecycleAsync(ApiKeyLifecycleContext ctx) => ctx.Reason switch
    {
        ApiKeyLifecycleReason.Deleted => store.RemoveAsync(ctx.ApiKeyId),
        _ => store.SaveAsync(ctx.ApiKeyId, protector.Protect(ctx.PrivateToken), ctx.TeamKey, ctx.Tags),
    };
}

builder.AddThargaPlatform(o => o.AddApiKeyLifecycleHandler<MyHandler>());
```

A throwing handler propagates out of the originating operation (capture failures are not swallowed). You own whatever you capture — encrypt it at rest.

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

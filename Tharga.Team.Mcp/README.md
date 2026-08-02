# Tharga Team Mcp
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Mcp)](https://www.nuget.org/packages/Tharga.Team.Mcp)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Mcp)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Team bridge for [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp). Connects MCP tool and resource invocations to Tharga.Team's authentication, scope enforcement, and audit logging.

> **Renamed from `Tharga.Platform.Mcp`.** NuGet package IDs cannot be renamed, so this is a new ID and the
> old one is deprecated. Migrating is a deliberate step with two breaking changes: `mcp.AddPlatform()` is now
> `mcp.AddTeam()`, and **every resource URI moved from `platform://` to `team://`** — update any MCP client
> that references them by string.

## What it does

- Populates `IMcpContext` from the authenticated `HttpContext.User` (works with both OIDC and API Key authentication)
- Enforces provider scope class: `McpScope.System` requires `Roles.Developer`, `McpScope.Team` requires team membership
- Emits audit log entries for every MCP tool invocation — success and failure
- Registers built-in `mcp:*` scopes in **both** of Team's scope registries — see below
- Requires authentication on the MCP endpoint — anonymous requests are rejected

## Quick start

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddTeam();        // bridge to Team auth/scopes/audit
    // ... other provider packages (e.g. mcp.AddMongoDB())
});

app.UseThargaMcp();
```

## Granting `mcp:discover`

`AddTeam()` registers the built-in `mcp:*` scopes in both the **team** and the **system** registry,
because both routes to holding one are legitimate. `mcp:discover` can therefore be granted three ways:

| Route | How | Where it authorizes |
|---|---|---|
| Access level | Registered at `AccessLevel.Viewer`, so any member at Viewer or above holds it | Inside the caller's **selected team** |
| App role | `o.ConfigureSystemRoles = r => r.Map("Developer", McpScopes.Discover);` | System-wide, no team needed |
| System API key | Grant it when minting the key | System-wide, no team needed |

`IMcpScopeChecker` accepts either provenance. A **system** grant authorizes with no team selected; a
**team** grant authorizes only alongside a `TeamKey` claim — a scope claim without a team context is not
a grant, and is refused.

```csharp
public sealed class MyTool(IMcpScopeChecker scopeChecker)
{
    public Task<string> ListAsync()
    {
        scopeChecker.Require(McpScopes.Discover);   // throws UnauthorizedAccessException if not held
        ...
    }
}
```

`IMcpScopeChecker` is an opt-in helper for tools that want to enforce a scope imperatively; nothing in
this package or `Tharga.MongoDB.Mcp` calls it for you.

> **Behaviour change.** `mcp:discover` was previously registered as a team scope but checked only against
> system claims, so an access-level grant could never satisfy it and a tool calling
> `Require(McpScopes.Discover)` rejected every caller — including a team Owner. If you wrote a test
> asserting that rejection, it now passes the caller instead.

## Authentication

`mcp.AddTeam()` contributes the API-key scheme to the MCP endpoint, so the default
`ThargaMcpOptions.RequireAuth = true` accepts an API key with no further configuration:

```csharp
builder.Services.AddThargaMcp(mcp => mcp.AddTeam());
app.UseThargaMcp();
```

That matters because MCP callers are agents — there is normally no user, so an API key is the expected
credential. Before `Tharga.Mcp` 1.0.1, `RequireAuth` emitted a policy naming no scheme, which
authenticated against the application's **default** scheme; in a Blazor host that is OIDC, so a valid key
was answered with a 302 to a login page ([Tharga/Mcp#18](https://github.com/Tharga/Mcp/issues/18)).

Add your own scheme alongside if you also want a signed-in user to reach the endpoint:

```csharp
mcp.Options.AuthenticationSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
```

## User and team resources

Always-on resource providers that surface the authenticated caller's own data. Both providers self-gate on the principal's claims, so anonymous and system-only callers see no resources.

### User scope (`McpScope.User`)

| URI | Contents |
|-----|----------|
| `team://me` | The caller's `IUser` (`key`, `identity`, `name`, `email`) and a `memberships` array — for each team the caller is in, its `teamKey`, `teamName`, plus the caller's `accessLevel` and membership `state`. |

Listed when the principal carries a `NameIdentifier` (or equivalent) claim. Read fails with `UnauthorizedAccessException` if `IUserService.GetCurrentUserAsync` returns null.

### Team scope (`McpScope.Team`)

| URI | Contents |
|-----|----------|
| `team://team` | Metadata for the team the call addresses — the one named in `X-Team-Key`, else the `TeamKey` claim: `key`, `name`, `icon`, `consentedRoles`. |
| `team://team/members` | Members of that team: `key`, `name`, `accessLevel`, `state`, `tenantRoles`, `scopeOverrides`, and an `invited` flag. |
| `team://team/apikeys` | API keys for that team. Raw key values are redacted (the `apiKey` property is omitted entirely). Listed only when `IApiKeyAdministrationService` is registered. |

Listed when the principal carries a `TeamKey` claim **or names a team on the call** (below). Read fails with `UnauthorizedAccessException` if neither applies.

## Naming a team on the call

Send the team key in an `X-Team-Key` header:

```http
POST /mcp
Authorization: Bearer <api key>
X-Team-Key: acme-corp
```

Everything on this surface used to derive from the caller's `TeamKey` claim, so a call could only address
the team the caller was already anchored to — and **a system key is anchored to none**, so it could
address no team at all. That also made team consent unreachable here: there was no team whose consented
level could be resolved. The Blazor surface has done this since 3.2.0; this is parity, not new semantics.

**Per call, not per session.** `ModelContextProtocol` 2.0.0 is stateless by default, so there is no
session to hold a selection in — and over HTTP, per-request *is* per-call. The header is read once, in
`HttpContextMcpContextAccessor`; providers keep reading `context.TeamId` and need no change.

### What naming a team grants

| Caller | Gets |
|---|---|
| A member of the named team | Their membership scopes in it |
| Not a member, but holds a global role the team consented to | The team's consented level |
| Neither, or the team does not exist | **Refused** |
| A suspended member | **Refused** — suspension means no access, by any door |

**Selection replaces, it never accumulates.** Scopes are recomputed for the named team; the principal's
own scope claims describe a *different* team and are never consulted, so naming a team can only narrow
what a caller may do. System scopes are unaffected, being team-independent.

The rule comes from the same resolver the Blazor claims builder uses, so a caller reaches a team at the
same level over MCP as through the UI.

### Refused, not empty

Naming a team you cannot reach throws rather than returning an empty result. Everywhere else on this
surface seeing nothing is the correct answer; here it would be a misleading one — the caller asked about
a specific team, and an empty answer reads as *"that team is empty"* rather than *"you cannot see it"*.

Change the header name with `o.TeamKeyHeader` if it collides with something in your stack.

### Consent is configured in one place

The default level a team's consent grants lives on `ConsentOptions` in the **`Tharga.Team`** package, and
is registered once for every surface to read:

```csharp
builder.AddThargaTeam(o => o.Blazor.Consent.AccessLevel = AccessLevel.User);
```

It is deliberately not an MCP option. Consent decides what a caller may do in a team they do not belong
to, and a second copy here would let the same caller reach the same team at two different levels
depending on which door they came through — which is exactly what happened before it was moved.

## System-scope diagnostic resources (opt-in)

Expose read-only diagnostic data under `team://system/*` for callers with the Developer role. Non-developers see no resources and get `UnauthorizedAccessException` on read.

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddTeam(o =>
    {
        o.ExposeSystemResources = true;
    });
});
```

Available resources (listed only when the matching dependency is registered):

| URI | Contents |
|-----|----------|
| `team://system/apikeys` | System API keys (not bound to a team). Raw key values are redacted. |
| `team://system/roles` | Tenant roles registered via `AddThargaTenantRoles` |
| `team://system/audit` | Most recent ~100 audit entries from the last 7 days |

Per-team API-key listings now ship under `team://team/apikeys` (see "Team scope" above). Cross-tenant team listings remain deferred — they require a new `ITeamService.GetAllTeamsAsync` method.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp) | MCP foundation (contracts, transport) |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Team scope/audit primitives |

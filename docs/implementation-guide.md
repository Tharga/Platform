# Tharga Platform — Implementation Guide

Step-by-step instructions for adding Tharga Platform features to a Blazor application.

## Recommended: Single-call setup

For most applications, use `AddThargaPlatform` to register everything in one call:

```csharp
using Tharga.Team.Blazor.Framework;

builder.AddThargaPlatform(o =>
{
    o.Blazor.Title = "My App";
    o.Blazor.RegisterTeamService<MyTeamService, MyUserService>();

    // Optional: scopes, roles, audit
    o.ConfigureScopes = scopes => { /* ... */ };
    o.ConfigureTenantRoles = roles => { /* ... */ };
    o.Audit = new AuditOptions();
});

// MongoDB persistence (always separate — requires your entity types)
builder.Services.AddMongoDB(o => { /* connection config */ });
builder.Services.AddThargaTeamRepository(o =>
{
    o.UseUserEntity<MyUserEntity>();
    o.UseTeamEntity<MyTeamEntity, MyTeamMember>();
});

var app = builder.Build();
app.UseThargaPlatform();
```

This replaces Steps 1–8 below. Set sub-options to `null` to skip features you don't need (e.g. `o.Controllers = null`, `o.ApiKey = null`).

---

## Advanced: Step-by-step setup

Use the individual `Add*` methods when you need partial or custom registration. Each step is a self-contained feature that builds on previous steps. Add only what you need.

> **Secrets:** Several steps require sensitive configuration values (client IDs, connection strings, API keys).
> These should never be committed to source control. Use **Manage User Secrets** in Visual Studio
> (right-click the Server project > Manage User Secrets) or run `dotnet user-secrets init` followed by
> `dotnet user-secrets set "Section:Key" "value"` from the Server project directory.

---

## Dependency overview

```
Step 1: UI Foundation (Tharga.Blazor)
    │
Step 2: Authentication (Tharga.Team.Blazor)
    │
    ├── Step 3: API Controllers & Swagger (Tharga.Team.Service)
    │
    ├── Step 4: Team Management (Tharga.Team.Blazor + Tharga.Team.MongoDB)
    │       │
    │       ├── Step 5: API Key Authentication (Tharga.Team.Service)
    │       │
    │       ├── Step 6: Scopes (Tharga.Team + Tharga.Team.Service)
    │       │       │
    │       │       └── Step 7: Tenant Roles (Tharga.Team)
    │       │
    │       └── Step 8: Audit Logging (Tharga.Team.Service)
```

---

## Step 1: UI Foundation

Adds Radzen-based UI components: buttons, breadcrumbs, error boundary, loading indicators, and layout primitives.

### Packages

```
dotnet add package Tharga.Blazor
```

### Program.cs (Server)

```csharp
using Tharga.Blazor.Framework;

builder.Services.AddRadzenComponents();
builder.Services.AddRadzenCookieThemeService(o =>
    o.StorageKeyName = "ThemeStorageName");
builder.Services.AddThargaBlazor(o => o.Title = "My App");
```

`AddThargaBlazor` registers `BreadCrumbService`, `BlazoredLocalStorage`, and `BlazorOptions`. It also supports binding from `appsettings.json`:

```json
{
  "Tharga": {
    "Blazor": {
      "Title": "My App"
    }
  }
}
```

```csharp
builder.Services.AddThargaBlazor(configuration: builder.Configuration);
```

Code configuration takes precedence over `appsettings.json`.

### Program.cs (Client — if using WebAssembly)

```csharp
builder.Services.AddRadzenComponents();
builder.Services.AddThargaBlazor();
```

### _Imports.razor (both projects)

```razor
@using Radzen
@using Radzen.Blazor
@using Tharga.Blazor
@using Tharga.Blazor.Framework
@using Tharga.Blazor.Framework.Buttons
@using Tharga.Blazor.Features.BreadCrumbs
```

### App.razor

Add to `<head>`:
```html
<RadzenTheme Theme="material" />
```

Add to `<body>`:
```html
<script src="@Assets["_content/Tharga.Blazor/tharga.blazor.js"]"></script>
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

### What becomes available

| Component | Description |
|-----------|-------------|
| `<ActionButton>` | Button with built-in busy state and error handling |
| `<CancelButton>` | Cancel button |
| `<CopyButton>` | Copy-to-clipboard button |
| `<StandardButton>` | General purpose button |
| `<BreadCrumbs>` | Breadcrumb navigation (registered by `AddThargaBlazor`) |
| `<Title>` | Page title (reads from `BlazorOptions.Title`) |
| `<CustomErrorBoundary>` | Error boundary with correlation ID |
| `<ExpandableCard>` | Collapsible card |
| `<Loading>` | Loading indicator — use instead of hardcoded "Loading..." text |
| `<DateTimeView>` | Formatted date/time display |
| `<TimeSpanView>` | Formatted time span display |

### Layout

Replace the default Bootstrap layout with Radzen layout components: `RadzenLayout`, `RadzenHeader`, `RadzenSidebar`, `RadzenBody`, `RadzenFooter`, `RadzenPanelMenu`.

### Verification

The app should render with Radzen styling. Buttons and layout components should work without errors.

---

## Step 2: Authentication

Adds Azure AD (CIAM) authentication with Cookie + OIDC, login/logout endpoints, and auth UI components.

**Requires:** Step 1

### Packages

```
dotnet add package Tharga.Team.Blazor
```

> `Microsoft.Identity.Web` is included transitively — no need to add it separately.

### Configuration

Add an `AzureAd` section to `appsettings.json`. Values are environment-specific:

```json
{
  "AzureAd": {
    "Authority": "",
    "ClientId": "",
    "TenantId": "",
    "CallbackPath": ""
  }
}
```

- **Authority** — varies by identity provider (e.g. CIAM: `https://<tenant>.ciamlogin.com/<domain>`, standard Entra ID: `https://login.microsoftonline.com/<tenant-id>/v2.0`)
- **ClientId** — from the Azure app registration
- **TenantId** — from the Azure app registration
- **CallbackPath** — varies by setup (e.g. `/signin-oidc`, `/authentication/login-callback`)

> **Secrets:** `ClientId` and `TenantId` may be considered sensitive depending on your environment.
> Put them in **Manage User Secrets** if you prefer not to commit them.

### Program.cs

```csharp
using Tharga.Team.Blazor.Features.Authentication;

// Service registration
builder.AddThargaAuth();

// After builder.Build()
app.UseThargaAuth();
```

### Options

```csharp
builder.AddThargaAuth(o =>
{
    o.LoginPath = "/sign-in";              // default: "/login"
    o.LogoutPath = "/sign-out";            // default: "/logout"
    o.ValidateConfiguration = false;       // default: true — throws at startup if AzureAd section is missing
});
```

### _Imports.razor

```razor
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Tharga.Team.Blazor.Features.Authentication
@using Tharga.Team.Blazor.Framework
```

> **Note:** `Tharga.Team.Blazor.Framework` provides the `Roles` class used in `AuthorizeView` (e.g. `Roles.Developer`, `Roles.TeamMember`).

### What becomes available

| Component | Namespace | Description |
|-----------|-----------|-------------|
| `<LoginDisplay />` | `Tharga.Team.Blazor.Features.Authentication` | Profile menu with Gravatar when authenticated, login button when not. Navigates to `/login`, `/logout`, and profile/team pages. |
| `<UserProfileView />` | `Tharga.Team.Blazor.Features.User` | Displays user's Gravatar, profile info, and authentication claims in an expandable card. |

### Usage

Add `<LoginDisplay />` to `NavMenu.razor` header.

Create a profile page:
```razor
@page "/profile"
@using Tharga.Team.Blazor.Features.User
@attribute [Authorize]

<UserProfileView />
```

### Version notes

- `UseThargaAuth()` requires **>= 2.0.1-pre.1** for correct async login behavior. Version 2.0.0 used `Results.Challenge` (synchronous) which caused DNS errors with some Azure AD configurations.

### Verification

The login button should appear. Clicking it redirects to Azure AD. After login, the profile menu shows with the user's Gravatar.

---

## Step 3: API Controllers & Swagger

Adds MVC controller support with OpenAPI documentation and Swagger UI.

**Requires:** Step 2

### Packages

```
dotnet add package Tharga.Team.Service
```

### Program.cs

```csharp
// Service registration
builder.Services.AddThargaControllers();

// After builder.Build()
app.UseThargaControllers();
```

### Options

```csharp
builder.Services.AddThargaControllers(o =>
{
    o.SwaggerTitle = "My API v1";          // default: "API v1"
    o.SwaggerRoutePrefix = "api-docs";     // default: "swagger"
});
```

### What becomes available

- MVC controller routing
- OpenAPI endpoint with API key security scheme
- Swagger UI at `/<SwaggerRoutePrefix>`
- API key header convention (`X-API-KEY`)

### Usage

Create controllers as usual:
```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello");
}
```

### Verification

Navigate to `/swagger` — the Swagger UI should load with your controllers listed.

---

## Step 4: Team Management

Adds multi-tenant team management with MongoDB persistence, team selection, member management, and claims augmentation.

**Requires:** Step 2, and a MongoDB database (via [Tharga.MongoDB](https://www.nuget.org/packages/Tharga.MongoDB))

### Packages

```
dotnet add package Tharga.Team.MongoDB
```

> `Tharga.Team` is included transitively via `Tharga.Team.Blazor`.
> You also need `Tharga.MongoDB.Blazor` configured separately — see [Tharga.MongoDB docs](https://github.com/Tharga/MongoDB).

### Configuration

Add a MongoDB connection string to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": ""
  }
}
```

> **Secrets:** The connection string contains credentials. Put it in **Manage User Secrets**.

### Program.cs

```csharp
// Service registration
builder.Services.AddThargaTeamBlazor(o =>
{
    o.Title = "My App";
    o.AutoCreateFirstTeam = true;          // default: false — auto-creates a team for first-time users
    o.ShowMemberRoles = false;             // default: false — shows tenant role assignment in team UI
    o.ShowScopeOverrides = false;          // default: false — shows scope override controls in team UI
    o.RegisterTeamService<MyTeamService, MyUserService>();
});

builder.Services.AddThargaTeamRepository(o =>
{
    o.RegisterUserRepository<UserEntity>();
    o.RegisterTeamRepository<TeamEntity, TeamMember>();
});
```

> **Note:** `AddThargaTeamBlazor()` internally calls `AddThargaBlazor()`, so `BreadCrumbService` and `BlazoredLocalStorage` are registered automatically.

### Implementing the required types

You need to create entity and service types that extend the base classes:

```csharp
// Entities
public record UserEntity : EntityBase, IUser { ... }
public record TeamEntity : TeamEntityBase<TeamMember> { ... }
public record TeamMember : TeamMemberBase { ... }

// Services
public class MyTeamService : TeamServiceBase { ... }
public class MyUserService : UserServiceBase { ... }
```

See `Tharga.Team.MongoDB` base classes (`TeamEntityBase<T>`, `TeamMemberBase`, `UserServiceRepositoryBase`) for the abstract members to implement.

### _Imports.razor

```razor
@using Tharga.Team.Blazor.Features.Team
```

### What becomes available

| Component | Description |
|-----------|-------------|
| `<TeamSelector />` | Dropdown to switch between teams |
| `<TeamComponent />` | Full team management (create, rename, delete, members) |
| `<TeamInviteView />` | Pending invitation view |
| `<UsersView />` | Admin user list |
| `<ApiKeyView />` | API key management (requires Step 5) |
| `<AuditLogView />` | Audit log viewer (requires Step 8) |
| `Roles.TeamMember` | Role claim added to authenticated team members |
| `Roles.Developer` | Role for developer-only UI sections |

The `TeamClaimsAuthenticationStateProvider` automatically augments the authentication state with team claims (`TeamKey`, `AccessLevel`, scopes) based on the selected team.

> **Note:** Team management works without scopes or tenant roles. The `ShowMemberRoles` and `ShowScopeOverrides` options only take effect when the corresponding registries are registered (Step 6 and Step 7). Without them, the team UI shows access levels only — which is sufficient for many applications.

### Claims Enrichment

Team, role, access level, and scope claims are automatically enriched on the `ClaimsPrincipal` when a team is selected. Platform provides two enrichment paths:

| Path | How it works | Hosting models |
|------|-------------|----------------|
| **Server-side** (default) | `IClaimsTransformation` reads the `selected_team_id` cookie during the HTTP pipeline | Blazor Server, SSR, Hybrid |
| **Client-side** | `AuthenticationStateProvider` decorator reads from LocalStorage via JS interop | Standalone WASM only |

The server-side path is **always registered** — no configuration needed. It adds:
- `team_id` — selected team key
- `TeamKey` — team key claim
- `Role: TeamMember` — membership role
- `Role: Team{AccessLevel}` — access level role (e.g. `TeamOwner`, `TeamAdministrator`)
- `AccessLevel` — raw access level value
- Scope claims — all effective scopes for the member's access level, roles, and overrides

#### `SkipAuthStateDecoration` (default: `true`)

This setting controls whether the client-side enrichment path is also registered:

- **`true` (default)** — Only server-side enrichment. Works for **Blazor Server, SSR, and Hybrid** apps. No JS interop is used. This is the recommended setting for most applications.
- **`false`** — Additionally registers a client-side `AuthenticationStateProvider` decorator that enriches claims via LocalStorage/JS interop. Only needed for **standalone Blazor WebAssembly** apps that have no server-side HTTP pipeline.

> **Warning:** Setting `SkipAuthStateDecoration = false` on a Server/SSR app will cause a blank page (silent deadlock from JS interop during prerendering).

#### Which setting do I need?

| App type | Setting |
|----------|---------|
| Blazor Server | `true` (default) — no config needed |
| Blazor Server with SSR | `true` (default) — no config needed |
| Blazor Hybrid (Server + WASM) | `true` (default) — server enriches claims for all render modes |
| Standalone Blazor WASM | `false` — needs client-side enrichment |

### Verification

After login, the team selector should appear. Creating a team should persist to MongoDB. Switching teams should update the claims.

---

## Step 5: API Key Authentication

Adds API key authentication so external clients can call your API using `X-API-KEY` headers.

**Requires:** Step 3, Step 4

### Program.cs

Extend the existing `AddThargaTeamBlazor` call to register the API key service, and add API key authentication:

```csharp
builder.Services.AddThargaTeamBlazor(o =>
{
    // ... existing team config ...
    o.RegisterApiKeyAdministrationService<MyApiKeyService>();
});

builder.Services.AddThargaApiKeys();

// Chain onto the existing authentication registration:
builder.Services.AddAuthentication()
    .AddThargaApiKeyAuthentication();
```

### Options

```csharp
.AddThargaApiKeyAuthentication(o =>
{
    o.AdvancedMode = false;                // default: false — simple mode auto-generates keys
    o.AutoKeyCount = 2;                    // default: 2 — number of auto-generated keys in simple mode
    o.AutoLockKeys = false;               // default: false — auto-lock keys after creation
    o.MaxExpiryDays = 365;                // default: null — maximum key expiry in days
});
```

### What becomes available

- API key authentication handler (validates `X-API-KEY` header)
- `[Authorize(Policy = "ApiKeyPolicy")]` attribute for controllers
- API key management UI via `<ApiKeyView />` (from Step 4)
- Constants in `ApiKeyConstants.HeaderName`, `ApiKeyConstants.PolicyName`

### _Imports.razor (if referencing constants)

```razor
@using Tharga.Team.Service
```

### Verification

Create an API key via the UI, then call your API with `X-API-KEY: <key>` header. The request should authenticate successfully.

---

## Step 6: Scopes

Adds fine-grained permission scopes that control access to service methods. Scopes are resolved per team member based on their access level, tenant roles, and scope overrides.

**Requires:** Step 4

### Program.cs

```csharp
using Tharga.Team;
using Tharga.Team.Service;

// Define scopes with default minimum access levels
builder.Services.AddThargaScopes(scopes =>
{
    scopes.Register("feature:read", AccessLevel.Viewer);
    scopes.Register("feature:write", AccessLevel.User);
    scopes.Register("feature:manage", AccessLevel.Administrator);
});

// Register services with scope enforcement
builder.Services.AddScopedWithScopes<IMyService, MyService>();
```

### Service implementation

Decorate service methods with the required scope:

```csharp
public class MyService : IMyService
{
    [RequireScope("feature:read")]
    public Task<Data> GetAsync() { ... }

    [RequireScope("feature:write")]
    public Task SaveAsync(Data data) { ... }
}
```

The `ScopeProxy<T>` automatically checks that the current user has the required scope before calling the method. If the scope is denied, an `UnauthorizedAccessException` is thrown.

### How scopes are resolved

1. **Access level** — Owner and Administrator get all scopes. User gets scopes at User or Viewer level. Viewer gets only Viewer-level scopes.
2. **Tenant roles** — Additional scopes granted by assigned roles (see Step 7).
3. **Scope overrides** — Per-member overrides set in the team management UI (when `ShowScopeOverrides = true`).

### Built-in scopes

| Scope | Default level | Source |
|-------|---------------|--------|
| `team:read` | — | `TeamScopes.Read` |
| `team:manage` | — | `TeamScopes.Manage` |
| `member:invite` | — | `TeamScopes.MemberInvite` |
| `member:remove` | — | `TeamScopes.MemberRemove` |
| `member:role` | — | `TeamScopes.MemberRole` |
| `apikey:manage` | — | `ApiKeyScopes.Manage` |

### Alternative: Access level enforcement

For simpler cases where scopes are overkill, use access level enforcement instead:

```csharp
builder.Services.AddScopedWithAccessLevel<IMyService, MyService>();
```

```csharp
[RequireAccessLevel(AccessLevel.Administrator)]
public Task DeleteAsync(string id) { ... }
```

### Verification

Call a scope-protected method as a Viewer when it requires User level — it should be denied. Elevate the member's access level and retry — it should succeed.

---

## Step 7: Tenant Roles

Adds named roles that bundle scopes together, making it easier to manage permissions for team members.

**Requires:** Step 6

### Program.cs

```csharp
builder.Services.AddThargaTenantRoles(roles =>
{
    roles.Register("Editor", new[] { "feature:read", "feature:write" });
    roles.Register("Auditor", new[] { "feature:read", "audit:read" });
});
```

### Team UI

Set `ShowMemberRoles = true` in `AddThargaTeamBlazor` options to show role assignment controls in the team management UI:

```csharp
builder.Services.AddThargaTeamBlazor(o =>
{
    // ... existing config ...
    o.ShowMemberRoles = true;
});
```

### How it works

When a team member is assigned the "Editor" role, they automatically receive the `feature:read` and `feature:write` scopes in addition to their access-level scopes. Roles are combined — a member with both "Editor" and "Auditor" gets all scopes from both.

### Verification

Assign a role to a team member, then verify they can access methods protected by the role's scopes.

---

## Step 8: Audit Logging

Adds audit logging for service calls, authorization events, and data changes. Logs can be stored in the application logger, MongoDB, or both.

**Requires:** Step 4

### Program.cs

```csharp
builder.Services.AddThargaAuditLogging();
```

### Options

```csharp
builder.Services.AddThargaAuditLogging(o =>
{
    o.StorageMode = AuditStorageMode.Logger;           // default: Logger — options: Logger, MongoDB, Logger | MongoDB
    o.CallerFilter = AuditCallerFilter.Api | AuditCallerFilter.Web;  // default: Api | Web
    o.EventFilter = AuditEventFilter.All;              // default: All
    o.ExcludedActions = new[] { "read", "list" };      // default: empty — skip noisy read operations
    o.ExcludedEndpoints = Array.Empty<string>();       // default: empty
    o.RetentionDays = 90;                              // default: 90
    o.BatchSize = 100;                                 // default: 100 — for MongoDB batch writes
    o.FlushIntervalSeconds = 5;                        // default: 5 — for MongoDB flush interval
});
```

> **Note:** To use `AuditStorageMode.MongoDB`, you need MongoDB configured (from Step 4).

### What becomes available

| Component | Description |
|-----------|-------------|
| `<AuditLogView />` | Audit log viewer with charts and filtering |
| `IAuditLogger` | Injectable service for custom audit entries |

### Audit entry fields

Each audit entry captures: timestamp, correlation ID, event type, feature/action, caller identity, team key, access level, scope check results, duration, and custom metadata.

### Event types

| Type | When logged |
|------|------------|
| `ServiceCall` | Any proxied service method call |
| `AuthSuccess` | Successful authentication |
| `AuthFailure` | Failed authentication |
| `ScopeDenial` | Scope check denied |
| `DataChange` | Data modification |
| `RateLimit` | Rate limit hit |

### Verification

Perform some actions, then view the audit log via `<AuditLogView />`. Entries should appear with correct caller identity and scope information.

---

## Quick reference: Registration order in Program.cs

```csharp
using Microsoft.AspNetCore.Authentication;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Authentication;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.Service;

// Step 1: Radzen + Blazor foundation
builder.Services.AddRadzenComponents();
builder.Services.AddThargaBlazor(o => o.Title = "My App");

// Step 2: Authentication
builder.AddThargaAuth();

// Step 3: Controllers
builder.Services.AddThargaControllers();

// Step 4: Team management
builder.Services.AddThargaTeamBlazor(o =>
{
    o.Title = "My App";
    o.RegisterTeamService<MyTeamService, MyUserService>();
    o.RegisterApiKeyAdministrationService<MyApiKeyService>();  // Step 5
    o.ShowMemberRoles = true;                                   // Step 7
    o.ShowScopeOverrides = true;                                // Step 6
});
builder.Services.AddThargaTeamRepository(o =>
{
    o.RegisterUserRepository<UserEntity>();
    o.RegisterTeamRepository<TeamEntity, TeamMember>();
});

// Step 5: API key auth
builder.Services.AddThargaApiKeys();
builder.Services.AddAuthentication()
    .AddThargaApiKeyAuthentication();

// Step 6: Scopes
builder.Services.AddThargaScopes(scopes =>
{
    scopes.Register("feature:read", AccessLevel.Viewer);
    scopes.Register("feature:write", AccessLevel.User);
});
builder.Services.AddScopedWithScopes<IMyService, MyService>();

// Step 7: Tenant roles
builder.Services.AddThargaTenantRoles(roles =>
{
    roles.Register("Editor", new[] { "feature:read", "feature:write" });
});

// Step 8: Audit
builder.Services.AddThargaAuditLogging();

var app = builder.Build();

// Step 2: Auth endpoints
app.UseThargaAuth();

// Step 3: Controllers
app.UseThargaControllers();
```

---

## Quick reference: _Imports.razor

```razor
@* Step 1: UI Foundation *@
@using Radzen
@using Radzen.Blazor
@using Tharga.Blazor
@using Tharga.Blazor.Framework
@using Tharga.Blazor.Framework.Buttons
@using Tharga.Blazor.Features.BreadCrumbs

@* Step 2: Authentication *@
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Tharga.Team.Blazor.Features.Authentication
@using Tharga.Team.Blazor.Framework

@* Step 4: Team management *@
@using Tharga.Team.Blazor.Features.Team
```

---

## Package summary

| Package | Added in | Purpose |
|---------|----------|---------|
| `Tharga.Blazor` | Step 1 | Generic UI components (Radzen, buttons, breadcrumbs) |
| `Tharga.Team.Blazor` | Step 2 | Authentication, team UI, claims augmentation |
| `Tharga.Team.Service` | Step 3 | API controllers, API key auth, scopes, audit |
| `Tharga.Team.MongoDB` | Step 4 | MongoDB persistence for teams and users |
| `Tharga.Team` | (transitive) | Domain models, authorization primitives |

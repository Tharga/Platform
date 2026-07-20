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
| `<LoginDisplay />` | `Tharga.Team.Blazor.Features.Authentication` | Profile menu with Gravatar when authenticated, login button when not. Navigates to `/login`, `/logout`, and profile/team pages. The Team item can be restricted to specific roles via `TeamMenuRoles`. |
| `<UserProfileView />` | `Tharga.Team.Blazor.Features.User` | Displays user's Gravatar, profile info, and authentication claims in an expandable card. |

### Usage

Add `<LoginDisplay />` to `NavMenu.razor` header.

By default the **Team** item in the profile menu is shown to every authenticated user (whenever a team service is registered). To restrict it to specific roles, set `TeamMenuRoles` — the item is then shown only to users in at least one of those roles, and hidden for everyone else:

```razor
<LoginDisplay TeamMenuRoles="@(new[] { "Administrator", "Developer" })" />
```

Leaving `TeamMenuRoles` unset keeps the original behavior (visible to all authenticated users). This gates the menu *link* only — protect the `/team` page itself with `[Authorize(Roles = ...)]` as well.

Create a profile page:
```razor
@page "/profile"
@using Tharga.Team.Blazor.Features.User
@attribute [Authorize]

<UserProfileView />
```

### Localizing menu strings

The profile menu (`LoginDisplay`) and `TeamSelector` strings — *User, Team, Logout, Login, Create Team, Loading…* — resolve through `IThargaTextProvider`. By default they return English; register your own provider to translate them, e.g. by bridging to your app's content/localization system. Each string is a `TextKey` that bundles a stable key with its English fallback, and the keys live in `TeamMenuText`:

```csharp
public sealed class MyMenuText(IContentService content) : IThargaTextProvider
{
    // Return a translation for the key, or fall back to the bundled English default.
    public Task<string> GetAsync(TextKey key) => content.GetOrDefaultAsync(key.Key, key.Default);
}

// Register it through the platform options (same pattern as AddClaimsEnricher):
builder.AddThargaPlatform(o => o.Blazor.AddTextProvider<MyMenuText>());
```

Without a provider the English defaults are used, so this is non-breaking for existing apps.

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

### Customizing the OpenAPI document (.NET 10+)

`AddThargaControllers` owns the OpenAPI document and registers the API-key security scheme on it. To add your own `IOpenApiDocumentTransformer` / `IOpenApiOperationTransformer` — for example, per-scope operation filtering so the generated spec only exposes operations the caller is authorized for — use the `ConfigureOpenApi` hook rather than calling `AddOpenApi("v1", …)` directly:

```csharp
builder.Services.AddThargaControllers(o =>
    o.ConfigureOpenApi(api => api.AddDocumentTransformer<ScopeFilteringDocumentTransformer>()));
```

The callback receives the same `OpenApiOptions` Tharga configures, so your transformers apply to the document Tharga already manages. Multiple `ConfigureOpenApi` calls compose in registration order. Using this hook (instead of a separate `AddOpenApi("v1", …)` call) keeps composition explicit and avoids the .NET 10 OpenAPI source generator emitting an interceptor into your project. On .NET 9 the document is Swashbuckle-based and this hook is not present.

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
    o.CreateTeamPath = "/get-started";     // default: null — built-in "Create team" entry points navigate here instead of the bare create (see "Overriding the Create team action")
    o.ShowMemberRoles = false;             // default: false — shows tenant role assignment in team UI
    o.ShowScopeOverrides = false;          // default: false — shows scope override controls in TeamComponent (team-member UI). For ApiKeyView, opt in via the [Parameter] ShowScopeOverrides on the component itself; the two flags are intentionally independent.
    o.RegisterTeamService<MyTeamService, MyUserService>();
});

builder.Services.AddThargaTeamRepository(o =>
{
    o.RegisterUserRepository<UserEntity>();
    o.RegisterTeamRepository<TeamEntity, TeamMember>();
});
```

> **Custom collection names:** If you need to change the MongoDB collection names (e.g. when sharing a database with a legacy app), set `TeamCollectionName` and `UserCollectionName`:
> ```csharp
> builder.Services.AddThargaTeamRepository(o =>
> {
>     o.TeamCollectionName = "MyTeams";     // default: "Team"
>     o.UserCollectionName = "MyUsers";     // default: "User"
>     o.RegisterUserRepository<UserEntity>();
>     o.RegisterTeamRepository<TeamEntity, TeamMember>();
> });
> ```

> **Note:** `AddThargaTeamBlazor()` internally calls `AddThargaBlazor()`, so `BreadCrumbService` and `BlazoredLocalStorage` are registered automatically.

### Implementing the required types

You need to create entity and service types that extend the base classes:

#### Entities

```csharp
public record UserEntity : EntityBase, IUser
{
    public required string Key { get; init; }
    public required string Identity { get; init; }
    public required string EMail { get; init; }
    public string? Name { get; init; }  // populate from 'name' claim for display names
}

public record TeamEntity : TeamEntityBase<TeamMember>;

public record TeamMember : TeamMemberBase;
```

#### UserService

```csharp
public class MyUserService : UserServiceRepositoryBase<UserEntity>
{
    public MyUserService(AuthenticationStateProvider asp, IUserRepository<UserEntity> repo)
        : base(asp, repo) { }

    protected override Task<UserEntity> CreateUserEntityAsync(ClaimsPrincipal principal, string identity)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? "unknown";
        var name = principal.FindFirst("name")?.Value;
        return Task.FromResult(new UserEntity
        {
            Key = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            Identity = identity,
            EMail = email,
            Name = name
        });
    }
}
```

> **Tip:** Populate `IUser.Name` from the `name` claim — it's used for default team names and member display names. If not set, the display name is derived from the email (e.g. `john.doe@example.com` becomes `John Doe`).

#### TeamService

```csharp
public class MyTeamService : TeamServiceRepositoryBase<TeamEntity, TeamMember>
{
    public MyTeamService(IUserService us, ITeamRepository<TeamEntity, TeamMember> repo, IMongoDbServiceFactory msf)
        : base(us, repo, msf) { }

    protected override Task<TeamEntity> CreateTeam(string teamKey, string name, IUser user, string displayName = null)
    {
        return Task.FromResult(new TeamEntity
        {
            Key = teamKey,
            Name = name,
            Members =
            [
                new TeamMember
                {
                    Key = user.Key,
                    Name = displayName,           // resolved from IUser.Name or email
                    AccessLevel = AccessLevel.Owner,
                    State = MembershipState.Member
                }
            ]
        });
    }

    protected override Task<TeamMember> CreateTeamMember(InviteUserModel model)
    {
        // Invitation and State are auto-generated by the base class if not set.
        // You only need to set them here if you want custom behavior.
        return Task.FromResult(new TeamMember
        {
            Key = null,                           // assigned when the user accepts the invite
            Name = model.Name,
            AccessLevel = model.AccessLevel
        });
    }
}
```

> **Auto-generated fields:** When `CreateTeamMember` returns a member without `Invitation`, the base class auto-generates it using the model's email, a new GUID invite key, and the current timestamp. Similarly, `State` defaults to `MembershipState.Invited` if not set. You can still set these explicitly if you need custom behavior.

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
| `<ApiKeyView />` | API key management (requires Step 5). Shows **Created** and **Last used** columns per key, and a **Tags** column (chips for keys in `ChipTagKeys`, plus an `(i)` tooltip of all tags). Opt-in `[Parameter]` flags: `ShowAuditLogButton`, `ShowScopeOverrides` (Scopes column + create-card multi-select + Edit-Scopes dialog per row), `ChipTagKeys` |
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

#### Custom Claims Enricher

If you need to inject custom claims (e.g. global roles from a database) before team member lookup and consent evaluation, implement `ITeamClaimsEnricher` and register it:

```csharp
public class MyClaimsEnricher : ITeamClaimsEnricher
{
    private readonly IMyUserDatabase _db;

    public MyClaimsEnricher(IMyUserDatabase db) => _db = db;

    public async Task EnrichAsync(ClaimsIdentity identity)
    {
        var roles = await _db.GetGlobalRolesAsync(identity.Name);
        foreach (var role in roles)
        {
            if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }
}
```

Register via options:

```csharp
builder.Services.AddThargaTeamBlazor(o =>
{
    o.AddClaimsEnricher<MyClaimsEnricher>();
    // ...
});
```

Or via `AddThargaPlatform`:

```csharp
builder.AddThargaPlatform(o =>
{
    o.Blazor.AddClaimsEnricher<MyClaimsEnricher>();
});
```

The enricher runs **once per request** inside `TeamServerClaimsTransformation`, before member lookup and consent evaluation. It supports full dependency injection (constructor injection). Duplicate claims are automatically prevented.

> **When team claims refresh.** `TeamServerClaimsTransformation` is an `IClaimsTransformation`, so it runs during **HTTP authentication** — a page load or the establishment of a Blazor Server circuit — not on every interaction within a live circuit. In practice team claims are re-evaluated on page load and on team switch (switching teams forces a full reload). They are **not** re-evaluated while a user sits on a page: if a member is removed, their access level is lowered, or a team revokes consent, the affected user keeps their existing claims until their circuit is replaced — a reload, a team switch, a new tab, or re-authentication. This applies to the service-layer checks too, since `BlazorTeamPrincipalAccessor` falls back to the circuit's authentication state when there is no `HttpContext`. Hosts needing prompt revocation can register a [`RevalidatingServerAuthenticationStateProvider`](https://learn.microsoft.com/aspnet/core/blazor/security/server/) to revalidate the circuit on an interval.

**Use cases:**
- Assign global roles (e.g. `Developer`, `SystemAdministrator`) based on user identity
- Add custom claims from external systems before team consent is evaluated
- Enrich the principal with application-specific metadata

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
    o.MaxExpiryDays = 365;                // default: 365 — maximum key expiry in days (null = no cap)
    o.LastUsedThrottle = TimeSpan.FromMinutes(1); // default: 1 min — min interval between "last used" timestamp writes per key (TimeSpan.Zero = stamp every request)
    o.MinKeyLength = 32;            // default: 32 — alphanumeric chars in the key secret; fixed length unless MaxKeyLength is set (floor 24 ≈143-bit; team + system keys)
    o.MaxKeyLength = null;          // default: null — when set, the length is random in [MinKeyLength, MaxKeyLength] per key instead of fixed
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

### System-set tags

API keys can carry **system-set tags** — a key-value list (`IReadOnlyList<Tag>`, `record Tag(string Key, string Value)`) set by backend code at creation. Tags are **backend-only**: there's a `tags` parameter on `CreateKeyAsync`, no mutation API, and no input in the `ApiKeyView` create card — so an operator can't add or re-point them from the UI.

```csharp
await apiKeyManagementService.CreateKeyAsync(
    teamKey, "Firewall opener", AccessLevel.Custom,
    scopeOverrides: new[] { "firewall:open" },
    tags: new[] { new Tag("Type", "firewall"), new Tag("firewall.groupId", "ABC123") });
```

- **Surfaced as claims.** Each tag becomes a `tag.{Key}` claim on the authenticated principal (`TeamClaimTypes.TagPrefix = "tag."`) — no DB round-trip to read a key's binding. Because it's a *list*, a key may carry the same key twice (e.g. `Type=firewall` + `Type=PIM`), producing two `tag.Type` claims; read them with `user.FindAll("tag.Type")`.
- **Displayed read-only.** `ApiKeyView` shows all tags in an `(i)` tooltip; pass `ChipTagKeys` to render selected keys as chips (e.g. `ChipTagKeys="@(new[] { "Type" })"`).
- **Legacy data.** Pre-tags keys stored an empty `Tags` document; reads tolerate this automatically (it deserializes as no tags). To purge the legacy field, call `IApiKeyRepository.CleanLegacyTagsAsync()` once (server-side, safe to repeat).

### Lifecycle hook (capturing the private token)

The private API token is shown once at creation and is otherwise unrecoverable — it's never persisted, logged, or exposed programmatically. If a host needs to **capture and re-deliver** a key (e.g. minting a scoped key to hand out repeatedly), register an `IApiKeyLifecycleHandler`. It receives the token at the moment it exists — on **create** and **recycle/regenerate** — plus a tokenless **delete** signal so the host can purge its own copy.

```csharp
public class MyApiKeyHandler(ISecretProtector protector, IMyKeyStore store) : IApiKeyLifecycleHandler
{
    public async Task OnApiKeyLifecycleAsync(ApiKeyLifecycleContext ctx)
    {
        switch (ctx.Reason)
        {
            case ApiKeyLifecycleReason.Created:
            case ApiKeyLifecycleReason.Recycled:
                await store.SaveAsync(ctx.ApiKeyId, protector.Protect(ctx.PrivateToken), ctx.TeamKey, ctx.Tags);
                break;
            case ApiKeyLifecycleReason.Deleted:
                await store.RemoveAsync(ctx.ApiKeyId);
                break;
        }
    }
}

// register it inside AddThargaPlatform:
builder.AddThargaPlatform(o =>
{
    // ...
    o.AddApiKeyLifecycleHandler<MyApiKeyHandler>();   // may be called multiple times
});
```

- **What you get** — `ApiKeyLifecycleContext`: `Reason`, `ApiKeyId` (the stable public id), `PrivateToken` (non-null on Created/Recycled, null on Deleted), `TeamKey` (null for system keys), `IsSystemKey`, `Name`, `Tags`. Applies to both team and system keys.
- **Error policy** — if the handler throws, the originating `CreateKey`/`RefreshKey`/`DeleteKey` throws too (capture failures are not swallowed). Note this does **not** roll back: a thrown create still leaves the key in storage and a thrown recycle has already rotated the secret — treat a failure as "operation failed" and reconcile (re-recycle, or delete the orphan).
- **Scope** — fires only on explicit create/recycle/delete. Simple-mode *auto-generated* keys (created lazily by `GetKeysAsync`) and lock/scope/role edits do **not** fire it.
- **Security** — the token is handed only to in-process handlers you registered; it is still never persisted or logged by the platform. You own whatever you capture (encrypt it at rest).
- Multiple handlers can be registered; all are invoked.

### Private (owner-scoped) keys

By default every team key is visible to all team admins and any Owner can recycle/lock/delete it. For keys that gate *personal* data, mint an **owner-scoped ("private")** key — bound to a team member, hidden from others, and mutable only by its owner.

- **Mint** — server-side via `IApiKeyAdministrationService.CreateKeyAsync(..., ownerMemberKey: currentMember.Key)`, or from the UI via the "Private (only me)" toggle (which calls `IApiKeyManagementService.CreateKeyAsync(ownerScoped: true)`; the service forces the owner to the *caller's own* member key — a caller can't mint a key owned by someone else).
- **Visibility / mutation** are enforced in `ApiKeyManagementService` from the authenticated principal (a `MemberKey` claim, added by the claims transformation):
  - **Owner** sees and manages their own private keys.
  - **Developer role** sees and manages all (audit/incident escape).
  - **Privileged access levels** (Administrator/Owner) can *see* private keys **only when the host opts in** — and remain **view-only** (they cannot recycle/lock/delete others').
- **`ApiKeyView` parameters** — `ShowPrivateKeys` (`None` default / `Mine` / `All`) and `AllowPrivilegedAccess` (default false; only meaningful with `All`). The actual visibility is always intersected with the caller's identity server-side, so the flags can never reveal a key the caller isn't entitled to.
- Existing keys have a null `OwnerMemberKey` (team-wide) — behaviour is unchanged unless you opt in. Not to be confused with **system keys** (team-less infra keys via `SystemApiKeyView`); private keys are still team-scoped.

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

> **Works in interactive Blazor Server too.** The proxy resolves the caller via `ITeamPrincipalAccessor`. The default implementation reads `IHttpContextAccessor` (controllers/API). `AddThargaPlatform` / `AddThargaTeamBlazor` automatically swap in a circuit-aware accessor that uses `HttpContext` when present and falls back to `AuthenticationStateProvider` otherwise — so a single `[RequireScope]` / `[RequireAccessLevel]` enforces both your API and interactive Blazor callers (no `HttpContext` is needed in a circuit). To plug in a different principal source, register your own `ITeamPrincipalAccessor`.

### How scopes are resolved

1. **Access level** — Owner and Administrator get all scopes. User gets scopes at User or Viewer level. Viewer gets only Viewer-level scopes. **`Custom` gets no base scopes** (and is exempt from the Owner/Administrator "all scopes" rule).
2. **Tenant roles** — Additional scopes granted by assigned roles (see Step 7).
3. **Scope overrides** — Per-member overrides set in the team management UI (when `ShowScopeOverrides = true`).

> **`AccessLevel.Custom` — least-privilege keys/members.** Use `Custom` when a principal should carry *only* its explicitly assigned roles and scope overrides, with nothing inherited from the access-level tier — e.g. a machine API key minted with a single scope. Its effective scopes are exactly `roles ∪ scopeOverrides`. Set it **explicitly**: a key created without an access level still defaults to a non-`Custom` level. `Custom` is surfaced in the `ApiKeyView` create card; it is intentionally hidden from the team-member pickers until member scope/role editing lands ([#76](https://github.com/Tharga/Platform/issues/76)).

### Built-in scopes

| Scope | Kind | Source | Gates |
|-------|------|--------|-------|
| `team:read` | team | `TeamScopes.Read` | View team details & members |
| `team:manage` | team | `TeamScopes.Manage` | Rename, delete, transfer ownership |
| `member:manage` | team | `TeamScopes.MemberManage` | Invite/remove members, change access level/roles/scope-overrides, edit display names |
| `teams:delete` | **system** | `SystemTeamScopes.Delete` | Delete **any** team (cross-team) |
| `teams:read` | **system** | `SystemTeamScopes.Read` | See **every** team (cross-team discovery) |
| `apikey:manage` | team | `ApiKeyScopes.Manage` | Create/refresh/lock/delete API keys |

#### Team-operation authorization

Team mutations are enforced in the **service layer** (`AuthorizationTeamServiceDecorator` over `ITeamService`), so the same rules protect the Blazor circuit **and** any consumer's REST controller that calls the service — the toolkit ships no controllers of its own.

| Operation | Allowed when |
|---|---|
| Create | authenticated **and** `AllowTeamCreation` (no scope — self-service) |
| Delete | (`team:manage` on the team **and** `AllowTeamCreation`) **or** `teams:delete` |
| Rename / Consent | `team:manage` on the team |
| Member invite/remove/role/overrides/display-name | `member:manage` on the team |
| Transfer ownership | Owner only |

Team scopes (`team:*`, `member:manage`) authorize only the caller's **own** team — the `TeamKey` claim must match the team being acted on, so an admin of one team can't act on another. `TeamComponent` mirrors this in the UI: because the scope is issued for the **selected** team only, the Rename and Delete buttons appear on the selected team's card and not on the other teams you belong to. Select a team to manage it. **`teams:delete`** is a **system** scope (toolkit-defined) that authorizes deleting *any* team regardless of membership and regardless of `AllowTeamCreation` — grant it to your support/dev tooling via `o.ConfigureSystemRoles` (e.g. map `Developer` → `teams:delete`) or to a system API key. Setting `AllowTeamCreation = false` disables the self-service create and in-team delete paths but never blocks `teams:delete`.

### Alternative: Access level enforcement

For simpler cases where scopes are overkill, use access level enforcement instead:

```csharp
builder.Services.AddScopedWithAccessLevel<IMyService, MyService>();
```

```csharp
[RequireAccessLevel(AccessLevel.Administrator)]
public Task DeleteAsync(string id) { ... }
```

> A `Custom` principal is the lowest tier and fails **every** `[RequireAccessLevel]` gate (including `Viewer`). Authorize such principals with scope-based checks (`[RequireScope]`) rather than access-level enforcement.

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

Role assignment is a **component parameter**, not a global option. Set `ShowRoles="true"` on `<TeamComponent>`
(and on `<ApiKeyView>` to assign roles to keys):

```razor
<TeamComponent TMember="MyMember" ShowRoles="true" ShowScopeOverrides="true" />
```

### How it works

When a team member is assigned the "Editor" role, they automatically receive the `feature:read` and `feature:write` scopes in addition to their access-level scopes. Roles are combined — a member with both "Editor" and "Auditor" gets all scopes from both. Members/keys store the role **names**; the scopes are resolved live from the registry (change a role's scopes and it applies to all assignees).

### Hiding roles per team

By default the role editor offers every registered role for every team. If a role is feature-gated — only meaningful for teams that have the feature enabled — register an `ITenantRoleVisibilityProvider` to hide it where the feature is off:

```csharp
public sealed class FeatureGatedRoleVisibility : ITenantRoleVisibilityProvider
{
    public Task<bool> IsRoleVisibleAsync(string teamKey, string roleName, CancellationToken ct = default)
        => _features.IsRoleEnabledForTeamAsync(teamKey, roleName, ct);
}

builder.Services.AddSingleton<ITenantRoleVisibilityProvider, FeatureGatedRoleVisibility>();
```

`<TeamComponent>` consults the provider per team before building each row's role list. This is **display-only**: a role already assigned to a member is preserved (never pruned) and still grants its scopes at runtime even while hidden — it simply isn't offered as a new choice, and reappears in the editor if the feature is re-enabled. The default provider shows all roles, so the hook is opt-in and non-breaking.

### Dynamic (runtime-defined) tenant roles

The roles registered above via `AddThargaTenantRoles` are **code roles** — global and fixed at deploy time. To let a team administrator define their **own** roles per team at runtime (e.g. org-specific Registrar / Case officer / Reader / Archivist), enable dynamic tenant roles and add the management component:

```csharp
builder.AddThargaPlatform(o =>
{
    o.ConfigureScopes = s => { s.Register("case:read", AccessLevel.Custom); s.Register("case:write", AccessLevel.Custom); };
    o.EnableDynamicRoles = true;
    // o.DynamicRoleManageScope = "access:manage"; // optional — scope for custom-role CRUD (default team:manage)
});
```

```razor
@* a team:manage-gated admin page *@
<TenantRoleManager />
```

- **Per-team storage** — custom roles are stored on the team document and edited via `ITeamManagementService.SetTeamCustomRolesAsync`, which requires **`team:manage`** on the team by default; set `o.DynamicRoleManageScope` to require a different scope (e.g. **`access:manage`**) instead, honoured by both the service layer and `TenantRoleManager`. Assigning a role to a member is still a **`member:manage`** operation.
- **No privilege escalation** — the manager only offers scopes registered via `o.ConfigureScopes`, and the server rejects any unregistered scope, duplicate names, or names that collide with a code role.
- **Resolved like code roles** — when enabled, a member assigned a custom role receives that role's scopes as claims (server, WASM, and API-key paths), and custom roles appear alongside code roles in the role pickers of `<TeamComponent>` (honouring the visibility provider above) and `<ApiKeyView ShowRoles="true">`, so a custom role can be assigned to a team API key.
- **Off by default** — `EnableDynamicRoles = false` leaves behaviour unchanged (code roles only).

### Verification

Assign a role to a team member, then verify they can access methods protected by the role's scopes.

---

## Step 7b: Managing roles & scopes (reference)

A principal's effective scopes are the **union** of four sources:

| Source | Applies to | Configured via |
|--------|-----------|----------------|
| **Access level** → scopes | team members, team API keys | `o.ConfigureScopes` (scope's default min level); `AccessLevel.Custom` grants no base scopes |
| **Tenant roles** → scopes | team members, team API keys | `o.ConfigureTenantRoles` (role → scopes) |
| **Scope overrides** (explicit) | team members, team API keys | per-principal, edited in the UI |
| **System scopes** (global, flat) | **system API keys**, and **users** via role mapping | `o.ConfigureSystemScopes`; `o.ConfigureSystemRoles` (app role → system scopes) |

All four surface as `Scope` claims, so service methods gate uniformly with `[RequireScope("…")]` regardless of whether the caller is a team member, a team key, a system key, or a privileged user.

> **Tip:** drop the `<ScopeView />` component (Tharga.Team.Blazor) on a page to explore the configured **team** scopes interactively. Pick an access level and roles and the scopes a member would have light up while the rest grey out; it defaults to the signed-in member's own access level, roles, and overrides (overrides are highlighted). It builds itself from `IScopeRegistry` / `ITenantRoleRegistry`, so it always matches the running configuration. When the signed-in user holds any **system** scopes, a separate **System scopes** table appears listing them (it's hidden entirely when they hold none; set `ShowSystemScopes="false"` to disable it) — so you can tell at a glance which of your scopes are team vs system.

### System scopes & privileged users

System scopes are global capabilities (no access-level hierarchy):

```csharp
o.ConfigureSystemScopes = s =>
{
    s.Register("system:teams:read", "Read any team's data (cross-tenant).");
    s.Register("system:metrics:read", "Read infrastructure metrics.");
};

// Map app/global roles to system scopes so privileged USERS gain them (team-independent).
o.ConfigureSystemRoles = r =>
{
    r.Map("Developer", "system:teams:read", "system:metrics:read", "apikey:manage", "audit:read");
};
```

- **System API keys** are minted with an explicit system-scope list (`SystemApiKeyView` picker reads `ConfigureSystemScopes`).
- **Users** with a mapped app role (e.g. `Developer`) receive the mapped scopes as claims via `TeamServerClaimsTransformation` — even with no team selected. Map `apikey:manage` / `audit:read` to a role to grant that role cross-team key/audit management.
- Map external IdP role claims to internal role names with an `ITeamClaimsEnricher` (runs first), e.g. `Dev → Developer`.

### Consent (cross-team access)

A team can **consent** to grant a global role access to its data, at a chosen access level:

```csharp
o.Blazor.Consent.Roles = ["Developer"];      // which roles a team may consent to
o.Blazor.Consent.ShowToggle = true;          // show the consent picker in TeamComponent
o.Blazor.Consent.AccessLevel = AccessLevel.Viewer; // default level when the consent doesn't carry one
```

The team admin picks the access level when consenting (Viewer/User/Administrator); a consented user gains that team's scopes at that level. The granted level is `team.ConsentAccessLevel ?? Consent.AccessLevel`.

When `ShowToggle` is on, the picker is shown to every member of the team but is **disabled** for anyone below `AccessLevel.Administrator` — so an ordinary member can see what the team has consented to without being able to change it.

### Cross-team visibility for oversight roles

Support and administration roles often need to see the whole estate. The `teams:read` system scope
grants exactly that — **discovery, and nothing else**:

- **Discovery is global.** A caller holding `teams:read` sees every team in `TeamComponent`,
  `TeamSelector` and the developer `UsersView` → Teams tab.
- **Access stays per-team and consent-governed.** Selecting a team they are not a member of grants only
  the scopes that team has consented to. A team that consented to nothing yields no access — the team is
  visible, its data is not.

Grant it either explicitly, or with the opt-in convenience flag:

```csharp
o.ConfigureSystemRoles = roles => roles.Map("Developer", SystemTeamScopes.Read);
// or, to reuse the consent role list:
o.Blazor.Consent.Roles = ["Developer"];
o.Blazor.Consent.GrantTeamsRead = true;   // default false
```

`GrantTeamsRead` is off by default on purpose. `Consent.Roles` means "roles a team *may grant access to*"
— a per-team, inbound opt-in. Turning that into a global enumeration privilege automatically would widen
access for existing hosts on upgrade, so it must be opted into. The flag composes with any
`ConfigureSystemRoles` mapping for the same role rather than conflicting with it.

**What a `teams:read` holder sees.** Each team carries a consent badge — *No access* (red), *Partial
access* (yellow, Viewer/User) or *Full access* (green, Administrator) — plus a **Not a member** badge on
teams they don't belong to. The `TeamSelector` shows the same state as a tinted dot.

**Selecting a team you don't belong to.** An oversight caller can select any team they can see, and that
choice is remembered across visits like any other — returning to the site re-selects it. Selection on its
own carries **no access**: the claims transformation still grants only what that team has consented to, to
a role the caller holds. No consent, or a role the team hasn't consented to, means no team scopes at all.

The distinction that matters is *chosen* versus *defaulted*. A team the caller picked is restored; a team
they never picked is never selected for them. When there is no current or remembered selection, the
fallback always comes from the caller's **own** memberships — so a support user with no memberships and no
prior choice lands on no team, rather than inside whichever tenant happens to sort first. A remembered team
that is no longer visible (deleted, consent revoked, scope removed) falls back the same way.

Team enumeration is deliberately **not audited** — it is a read with no side effect. Mutations performed
inside a team are audited as usual.

### Overriding the "Create team" action

By default the teamless **Create team** link (`TeamSelector`) navigates to `/team` and the **Create new Team** button (`TeamComponent`) calls `CreateTeamAsync()` directly. To route team creation through your own onboarding flow — instead of `AllowTeamCreation = false`, which hides the button *and* blocks the programmatic create API — use one of two override points, evaluated **callback → path → built-in**:

```csharp
// 1. Global, declarative — both entry points navigate to your page (which runs the wizard + CreateTeamAsync):
o.CreateTeamPath = "/get-started";
```

```razor
@* 2. Per component, imperative — handle in place (e.g. a dialog); takes precedence over CreateTeamPath: *@
<TeamSelector CreateTeamRequested="LaunchOnboardingAsync" />
<TeamComponent TMember="MyMember" CreateTeamRequested="LaunchOnboardingAsync" />
```

Both default to unset, so behavior is unchanged unless you opt in. The override applies to the built-in UI entry points only — teams created programmatically or via `AutoCreateFirstTeam` are unaffected.

### Component parameter reference

| Component | Parameters |
|-----------|-----------|
| `<TeamSelector>` | `CreateTeamRequested` (intercept the teamless "Create team" link) |
| `<TeamComponent>` | `ShowScopeTooltip` (default true), `ShowScopeOverrides`, `ShowRoles`, `CreateTeamRequested` (intercept the "Create new Team" button) |
| `<ApiKeyView>` | `ShowScopeTooltip` (true), `ShowScopeOverrides`, `ShowRoles`, `ShowLastUsed` (true), `ShowExpiryDatePicker`, `ShowTags` (`bool?`, null=auto), `ChipTagKeys`, `ShowAuditLogButton` |
| `<SystemApiKeyView>` | `ShowScopeTooltip` (true), `ShowScopeOverrides` (true), `ShowLastUsed` (true), `ShowExpiryDatePicker`, `ShowAuditLogButton` |

Access to manage keys is gated on `apikey:manage`; the audit log on `audit:read`. (The former per-component
`CrossTeamRoles` / `RequiredScopes` parameters were removed — grant cross-team access via the role→system-scope
mapping instead.)

---

## Step 8: Audit Logging

Adds audit logging for service calls, authorization events, and data changes. Logs can be stored in the application logger, MongoDB, or both.

**Requires:** Step 4

### Program.cs

```csharp
builder.Services.AddThargaAuditLogging();
```

> **⚠️ Most common gotcha:** `StorageMode` defaults to **`Logger` only**. The `AuditLogView` component
> reads from **MongoDB**, so with the default it stays **empty** — entries only go to `ILogger`. To
> populate the UI you must opt into Mongo storage:
> ```csharp
> builder.Services.AddThargaAuditLogging(o => o.StorageMode = AuditStorageMode.MongoDB);
> // or keep both: AuditStorageMode.Logger | AuditStorageMode.MongoDB
> ```
> `AuditStorageMode` is a `[Flags]` enum, so the values combine. MongoDB storage requires MongoDB
> configured (Step 4).

### Options

```csharp
builder.Services.AddThargaAuditLogging(o =>
{
    o.StorageMode = AuditStorageMode.Logger | AuditStorageMode.MongoDB; // default: Logger only — see gotcha above
    o.CallerFilter = AuditCallerFilter.Api | AuditCallerFilter.Web;  // default: Api | Web ([Flags])
    o.EventFilter = AuditEventFilter.All;              // default: All ([Flags])
    o.ExcludedActions = new[] { "read", "list" };      // default: empty — skip noisy read operations
    o.ExcludedEndpoints = Array.Empty<string>();       // default: empty
    o.RetentionDays = 90;                              // default: 90 — null (or <= 0) = keep forever (no TTL)
    o.BatchSize = 100;                                 // default: 100 — MongoDB background-writer batch size
    o.FlushIntervalSeconds = 5;                        // default: 5 — MongoDB background-writer flush interval
});
```

| Option | Default | Notes |
|---|---|---|
| `StorageMode` | `Logger` | `[Flags]`: `Logger`, `MongoDB`, or both. **Set `MongoDB` to populate `AuditLogView`.** |
| `CallerFilter` | `Api \| Web` | `[Flags]` — which caller sources to record. |
| `EventFilter` | `All` | `[Flags]` — which event types to record. |
| `ExcludedActions` | empty | Action names to skip (e.g. `"read"`, `"list"`). |
| `ExcludedEndpoints` | empty | Endpoints to skip (e.g. `"/health"`). |
| `RetentionDays` | `90` | `int?` mapped to a MongoDB TTL index (`Timestamp_TTL`). **`null` or `<= 0` = keep forever** (no TTL index). |
| `BatchSize` | `100` | Background MongoDB writer batch size. |
| `FlushIntervalSeconds` | `5` | Background MongoDB writer flush interval. |

> **Retention / TTL.** `RetentionDays` creates a MongoDB TTL index that auto-deletes entries older than
> the given age. Set it to **`null`** (or any value `<= 0`) to keep entries **indefinitely** — no TTL
> index is created. Caveat: MongoDB does not drop an existing TTL index automatically, so changing the
> retention on a collection that already has a `Timestamp_TTL` index (including switching to "forever")
> may require dropping that index manually.

### What gets audited

Mutations flow through auditing decorators: **team-service** operations (create/rename/delete team,
invite/remove member, set consent, …) and **API-key management** (create/recycle/lock/delete, for team
and system keys). Read operations pass through unaudited; use `ExcludedActions` to drop any others you
consider noise.

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

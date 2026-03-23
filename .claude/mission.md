# Mission: Tharga.Platform

Shared Blazor components and team management infrastructure, published as NuGet packages (Tharga.Blazor, Tharga.Team.Blazor, Tharga.Team.Service, Tharga.Team.MongoDB).

## Change Request: Simplify Registration and Improve Error Resilience

**Requested by:** Daniel Bohlin (Quilt4Net Server, PlutusWave — adoption of Platform 2.0.x)
**Date:** 2026-03-23

### Problem

Adopting Tharga Platform requires too many separate `Add*` calls, and when one is missing the app either crashes silently at startup or renders a blank page with no guidance. Three categories of issues were hit during Quilt4Net Server migration:

#### 1. Too many registration calls required
A consumer currently needs to call up to 7 separate methods: `AddThargaAuth()`, `AddThargaApiKeyAuthentication()`, `AddThargaApiKeys()`, `AddThargaTeamBlazor()`, `AddThargaTeamRepository()`, `AddThargaScopes()`, `AddThargaTenantRoles()`. Missing any one causes a runtime crash. There is no single entry point that sets up sensible defaults.

#### 2. Missing services crash without useful feedback
- `TeamComponent` has `[Inject] ITenantRoleRegistry` — if `AddThargaTenantRoles()` hasn't been called, the page crashes with an unhandled `InvalidOperationException`. The user sees a blank page; the error only appears in the browser console via `blazor.web.js`.
- `ApiKeyView` requires `IApiKeyManagementService` — not registered by `AddThargaApiKeys()`.
- `IScopeRegistry` was a hard `[Inject]` dependency (fixed in 2.0.1-pre.2 to null-safe, but the same pattern is not applied everywhere).

#### 3. MongoDB auto-registration silently misses Platform assemblies
`AddMongoDB()` scans assemblies by entry-assembly name prefix (e.g. `Quilt4Net`). `Tharga.Team.Service` doesn't match, so `IApiKeyRepository` and `IApiKeyRepositoryCollection` are never auto-registered. The consumer must know to call `o.AddAutoRegistrationAssembly(typeof(ApiKeyConstants).Assembly)` — this is undocumented and the resulting `AggregateException` at startup gives no hint about assembly scanning.

### Goal

Make it simple to adopt Platform with as few `Add*` registrations as possible, with sensible defaults that work out of the box. When something is misconfigured, provide clear in-context error messages instead of crashes.

### Requirements

1. **Provide a single top-level registration** (e.g. `AddThargaPlatform()`) that sets up all core services with default options. Individual `Add*` methods remain available for advanced/partial use.

2. **All services required by Blazor components must be registered by the same call that makes those components available.** If `AddThargaTeamBlazor()` exposes `TeamComponent` and `ApiKeyView`, it must also register their dependencies — or make them optional/null-safe.

3. **Graceful degradation for optional services.** Components should use `IServiceProvider.GetService<T>()` instead of `[Inject]` for optional dependencies (`ITenantRoleRegistry`, `IScopeRegistry`). When a required service is truly missing, render a clear in-page error message (e.g. "Tenant roles are not configured. Call `AddThargaTenantRoles()` in Program.cs.") instead of crashing.

4. **Platform packages should register their own assemblies for MongoDB auto-scanning.** `AddThargaApiKeys()` or `AddThargaTeamRepository()` should call `AddAutoRegistrationAssembly` for `Tharga.Team.Service` so consumers don't have to. The current behavior silently fails when the entry assembly has a different name prefix.

5. **Document the minimal registration path** in each package's README — one call to get started, with examples of customization.

### Context

Discovered during Quilt4Net Server migration to Platform 2.0.1-pre.1. These same issues will affect PlutusWave, FortDocs Web, and any other project adopting Platform. Each project currently needs to independently discover the correct combination of registration calls and workarounds.

## Bug: TeamComponent requires ITeamManagementService and scope claims but neither is auto-registered (2.0.1-pre.3)

**Reported:** 2026-03-23
**Found in:** Quilt4Net Server after upgrading Tharga.Team.* packages from 2.0.1-pre.2 to 2.0.1-pre.3

### Issue 1: ITeamManagementService not registered

`TeamComponent` has `[Inject] ITeamManagementService` but neither `AddThargaPlatform()` nor `AddThargaTeamBlazor()` registers it. The team page crashes with:

```
InvalidOperationException: Cannot provide a value for property 'TeamManagementService'
on type 'TeamComponent`1[...]'. There is no registered service of type
'Tharga.Team.ITeamManagementService'.
```

**Workaround (consumer side):**
```csharp
builder.Services.AddScoped<ITeamManagementService, TeamManagementService<TeamMemberModel>>();
```

**Fix:** `AddThargaTeamBlazor()` (or `AddThargaPlatform()`) should register `ITeamManagementService` → `TeamManagementService<TMember>` when `RegisterTeamService<>()` is called, since it knows the `TMember` type.

### Issue 2: No "simple mode" — management UI hidden without explicit scope registration

`TeamComponent` checks for scope claims (`team:manage`, `member:invite`, etc.) to show/hide management buttons. When `ConfigureScopes` is null (the default per docs: "scope registration is skipped"), no `IScopeRegistry` is registered, so no scope claims are added. As a result, team owners see only "Create team" — all management options (rename, delete, invite, remove member, change role) are hidden.

**Workaround (consumer side):**
```csharp
builder.Services.AddThargaScopes(scopes =>
{
    scopes.Register(TeamScopes.Read, AccessLevel.Viewer);
    scopes.Register(TeamScopes.Manage, AccessLevel.Administrator);
    scopes.Register(TeamScopes.MemberInvite, AccessLevel.Administrator);
    scopes.Register(TeamScopes.MemberRemove, AccessLevel.Administrator);
    scopes.Register(TeamScopes.MemberRole, AccessLevel.Administrator);
});
```
Plus adding scope claims in a custom `IClaimsTransformation`.

**Expected behavior:** When no scopes are configured, the `TeamComponent` should fall back to access-level-based visibility (Owner/Admin sees everything, User/Viewer sees limited options) — not hide all management UI. The built-in `TeamScopes` should be auto-registered as defaults by `AddThargaTeamBlazor()` so the team management UI works out of the box.

### Issue 3: IApiKeyManagementService not registered

`ApiKeyView` requires `IApiKeyManagementService` but `AddThargaApiKeyAuthentication()` / `AddThargaApiKeys()` only registers `IApiKeyAdministrationService`. The API key page shows: "API key management is not configured. Register IApiKeyManagementService in Program.cs to enable this view."

**Workaround (consumer side):**
```csharp
builder.Services.AddScoped<IApiKeyManagementService, ApiKeyManagementService>();
```
Plus registering `ApiKeyScopes.Manage` in the scope registry.

**Fix:** `AddThargaApiKeys()` should also register `IApiKeyManagementService` → `ApiKeyManagementService`, and `ApiKeyScopes.Manage` should be auto-registered in the scope registry defaults.

### Suggested fix

In `AddThargaTeamBlazor()` (when `RegisterTeamService<TTeam, TUser>()` is called):
1. Auto-register `ITeamManagementService` → `TeamManagementService<TMember>`
2. Auto-register `IScopeRegistry` with `TeamScopes` + `ApiKeyScopes` defaults (if not already registered)
3. Or: make `TeamComponent` and `ApiKeyView` fall back to `AccessLevel`-based visibility when no scope claims are present

In `AddThargaApiKeys()`:
4. Auto-register `IApiKeyManagementService` → `ApiKeyManagementService`

## Feature Request: Improve member invite flow and add email sending

**Requested by:** Daniel Bohlin
**Date:** 2026-03-23

### Current behavior

When inviting a member via the `TeamComponent`, email is mandatory and name is optional. No email is actually sent — the user must copy and share the invite link manually, but there is no indication of this.

### Requested changes

#### 1. Make email optional, name mandatory
- Name should be the required field (to identify the invited member in the team list)
- Email should be optional — when provided, an invitation email should be sent

#### 2. Email sending
- When email is provided and email sending is configured, send an invitation email with the invite link
- If email sending is not implemented or not configured, show a clear message such as: "Email sending is not configured. Copy the invite link below and send it manually."
- The invite link should always be available for manual copying regardless of email configuration

#### 3. Implement an email sender
- Add an `IEmailSender` (or similar) abstraction that consumers can configure
- Provide a default implementation (e.g. SMTP) or allow consumers to plug in their own
- Registration via `AddThargaPlatform()` options or a dedicated `AddThargaEmailSender()` call

## Feature Request: AuditLogView should show caller name, not ID

**Requested by:** Daniel Bohlin
**Date:** 2026-03-23

### Current behavior

The `AuditLogView` component displays `CallerIdentity` which is typically a user ID or API key identifier. This is too abstract — it's hard to tell who actually made the call.

### Requested change

Show the **name** of the caller instead of the raw ID:
- For **web/user calls**: resolve and display the user's display name (from `IUserService` or the `name`/`preferred_username` claim)
- For **API key calls**: display the API key's name (from `IApiKeyAdministrationService`)

The raw ID should still be available (e.g. as a tooltip or secondary column) for debugging, but the primary display should be human-readable.

## External References
- **Backlog**: `c:\Users\danie\SynologyDrive\Documents\Notes\Tharga\Toolkit\Platform.md`

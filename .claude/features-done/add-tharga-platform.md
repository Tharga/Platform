# Feature: Single Top-Level AddThargaPlatform Registration

## Goal
Provide a single entry point that sets up all core Platform services with sensible defaults, reducing 7+ calls to 1-2.

## Originating branch
develop

## Design

### AddThargaPlatform(WebApplicationBuilder, Action<ThargaPlatformOptions>)
Extends `WebApplicationBuilder` (required for auth). Composes:
- AddThargaAuth (auth + OIDC)
- AddThargaApiKeyAuthentication (API key auth scheme)
- AddThargaApiKeys (API key storage + repos)
- AddThargaTeamBlazor (Blazor components + services)
- AddThargaControllers (MVC + Swagger)
- AddThargaScopes (if configured)
- AddThargaTenantRoles (if configured)
- AddThargaAuditLogging (if configured)

### UseThargaPlatform(WebApplication)
Composes:
- UseThargaAuth
- UseThargaControllers

### ThargaPlatformOptions
Sub-options for each subsystem, all optional with sensible defaults.
Supports appsettings.json binding with code-override-wins.

### What's NOT included
- AddThargaTeamRepository — requires consumer-specific entity types, must be called separately
- AddMongoDB — infrastructure concern, consumer configures this

## Acceptance Criteria
- [ ] ThargaPlatformOptions class with sub-options for each subsystem
- [ ] AddThargaPlatform extension method on WebApplicationBuilder
- [ ] UseThargaPlatform extension method on WebApplication
- [ ] Tests verify all expected services are registered
- [ ] Individual Add* methods still work independently

## Done Condition
All acceptance criteria met, all tests pass.

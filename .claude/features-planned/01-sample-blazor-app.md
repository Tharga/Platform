# Feature: Sample Blazor Web App

## Goal
Add a sample Blazor Server app to the solution for testing and demonstrating Platform features during development.

## Scope
- New project: `Tharga.Platform.Sample` (or similar) — Blazor Server app
- References local projects directly (not NuGet packages)
- Configured with `AddThargaPlatform()` with all features enabled
- Pages for each team-scoped component:
  - Team management (TeamComponent, TeamSelector)
  - API keys (ApiKeyView)
  - Audit log (AuditLogView)
  - User profile (UserProfileView)
  - Users/admin view (UsersView)
- Local MongoDB for persistence (or in-memory fallback)
- AzureAd config via user-secrets (not committed)
- Not published as a NuGet package — excluded from pack/publish
- Added to the solution but not part of CI build artifacts

## Acceptance Criteria
- [ ] Sample app builds and runs locally
- [ ] All Platform components are reachable via navigation
- [ ] `AddThargaPlatform()` configured as a reference setup
- [ ] User-secrets used for AzureAd and MongoDB connection
- [ ] Project excluded from NuGet packaging

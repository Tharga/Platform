# Feature: Sample Blazor Web App

## Originating branch
develop

## Goal
Add a sample Blazor Server app to the solution for testing and demonstrating Platform features.

## Scope
- New project `Tharga.Platform.Sample` — Blazor Server, net10.0 only
- References local projects (Tharga.Team.Blazor, Tharga.Team.Service, Tharga.Team.MongoDB)
- `AddThargaPlatform()` with all features
- Pages for all team-scoped components
- Excluded from NuGet packaging and CI artifacts

## Acceptance Criteria
- [ ] Sample app builds and runs locally
- [ ] All Platform components reachable via navigation
- [ ] AddThargaPlatform() configured as reference setup
- [ ] User-secrets for AzureAd and MongoDB
- [ ] Excluded from NuGet packaging

## Done Condition
All acceptance criteria met, app builds successfully.

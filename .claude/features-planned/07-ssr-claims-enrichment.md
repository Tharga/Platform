# Feature: SSR Claims Enrichment (Request #2)

## Goal
Provide server-side claims enrichment for SSR Blazor apps that must skip TeamClaimsAuthenticationStateProvider.

## Scope
- When `SkipAuthStateDecoration = true`, register an `IClaimsTransformation` that enriches claims server-side
- Read selected team from cookie (same as what TeamClaimsAuthenticationStateProvider reads from LocalStorage)
- Add TeamKey, TeamMember role, Team{AccessLevel} roles, and scope claims
- Must handle re-entrance (IClaimsTransformation can be called multiple times per request)
- Wire into `AddThargaTeamBlazor()` or `UseThargaPlatform()` automatically
- From: Eplicta.FortDocs — Priority: High

## Acceptance Criteria
- [ ] SSR apps with `SkipAuthStateDecoration = true` get team claims on the server
- [ ] Re-entrance is handled correctly
- [ ] Claims match what TeamClaimsAuthenticationStateProvider would produce
- [ ] No JS interop required
- [ ] Tests cover the transformation with and without a selected team

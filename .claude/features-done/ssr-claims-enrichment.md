# Feature: SSR Claims Enrichment

## Originating branch
develop

## Goal
Server-side claims enrichment that works for Server, SSR, WASM, and hybrid Blazor apps.

## Approach
1. Always register an `IClaimsTransformation` that reads `selected_team_id` cookie and adds team/scope claims
2. Make `TeamClaimsAuthenticationStateProvider` SSR-safe (guard JS interop)
3. Both paths coexist — server gets claims from transformation, WASM from auth state provider
4. Deprecate `SkipAuthStateDecoration`

## Acceptance Criteria
- [ ] IClaimsTransformation registered automatically by AddThargaTeamBlazor
- [ ] Enriches: TeamKey, TeamMember role, Team{AccessLevel}, AccessLevel, scope claims
- [ ] Re-entrance handled (IClaimsTransformation can be called multiple times)
- [ ] TeamClaimsAuthenticationStateProvider is SSR-safe
- [ ] Works in Server, SSR, WASM, and hybrid modes
- [ ] Sample app's TeamCookieClaimsTransformation removed
- [ ] Tests cover transformation with and without a selected team

## Done Condition
All acceptance criteria met, sample app works without manual IClaimsTransformation.

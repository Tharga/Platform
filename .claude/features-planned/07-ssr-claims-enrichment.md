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

## Cleanup
- Remove `TeamCookieClaimsTransformation` from `Tharga.Platform.Sample` — it will no longer be needed once Platform registers its own `IClaimsTransformation`
- Consider changing `SkipAuthStateDecoration` default to `true` since SSR is the standard hosting model

## Important: Blocked functionality without this feature
With `SkipAuthStateDecoration = true` (required for SSR), scope claims are never added to the principal. This means:
- `_canManage` is always `false` in TeamComponent → delete team button never visible
- ApiKeyView access checks based on scopes fail
- AuditLogView scope-based access checks fail
- Any component relying on `TeamClaimTypes.Scope` claims is broken in SSR mode
This feature is the critical unblock for all scope-dependent UI in SSR apps.

## Acceptance Criteria
- [ ] SSR apps with `SkipAuthStateDecoration = true` get team claims on the server
- [ ] Re-entrance is handled correctly
- [ ] Claims match what TeamClaimsAuthenticationStateProvider would produce
- [ ] No JS interop required
- [ ] Tests cover the transformation with and without a selected team
- [ ] Sample app's `TeamCookieClaimsTransformation` removed

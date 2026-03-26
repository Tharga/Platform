# Feature: Auto-detect Blazor Hosting Model

## Goal
Remove the need for `SkipAuthStateDecoration` by automatically detecting whether the app uses Server, WASM, or Hybrid hosting — zero-config claims enrichment for all hosting models.

## The Problem

Platform needs to enrich the `ClaimsPrincipal` with team, role, access level, and scope claims when a team is selected. There are two enrichment paths:

1. **Server-side** — `TeamServerClaimsTransformation` (an `IClaimsTransformation`) reads the `selected_team_id` cookie from `HttpContext` during the HTTP pipeline. Works reliably for any request that goes through the server.

2. **Client-side** — `TeamClaimsAuthenticationStateProvider` (an `AuthenticationStateProvider` decorator) reads the selected team from LocalStorage via JS interop. Intended for WASM where there is no server HTTP pipeline.

The problem is that **path 2 causes a silent deadlock (blank page, no errors)** when used in Server/SSR apps. During SSR prerendering, Blazor calls `GetAuthenticationStateAsync()` on the server, and the JS interop call to LocalStorage hangs indefinitely — the browser JS runtime isn't connected yet.

## What Was Tried

### Attempt 1: `try/catch` around JS interop
The original code wrapped `_localStorage.GetItemAsStringAsync()` in a `try/catch`. **Result:** The call deadlocks rather than throwing — the `catch` is never reached.

### Attempt 2: `CancellationToken` with timeout
Added a `CancellationTokenSource` with a 200ms timeout to `GetItemAsStringAsync(key, cancellationToken)`. **Result:** The Blazored LocalStorage implementation does not honor the cancellation token. The call still deadlocks.

### Attempt 3: Check `IJSRuntime.IsInitialized` via reflection
`JSRuntime` has a `protected bool IsInitialized` property that is `false` during prerendering. Used reflection with `BindingFlags.NonPublic` to read it before calling JS interop. **Result:** Still caused a blank page. Either the property returns `true` in some SSR contexts, or the decorator registration itself (replacing `AuthenticationStateProvider`) disrupts the Blazor Server rendering pipeline.

### Attempt 4: Check `IHttpContextAccessor.HttpContext != null`
If `HttpContext` is available, we're on the server — skip JS interop. If null (WASM), use LocalStorage. **Result:** Still blank page. In Blazor Server, `HttpContext` may be null during the SignalR circuit (after the initial HTTP request), making this check unreliable. But more importantly, the auth state provider decoration itself appears to break SSR regardless of what happens inside `GetAuthenticationStateAsync`.

### Conclusion
The root cause appears to be the **auth state provider decoration pattern itself**, not just the JS interop call. Removing the existing `AuthenticationStateProvider` from the service collection and replacing it with a decorator disrupts the Blazor Server/SSR rendering pipeline in a way that causes a silent hang.

## What Works Now

- `SkipAuthStateDecoration = true` (default) — server-side `TeamServerClaimsTransformation` handles all Server/SSR/Hybrid cases via cookie. No JS interop, no decorator.
- `SkipAuthStateDecoration = false` — registers the auth state provider decorator for standalone WASM. Not tested with a WASM sample app yet.

## Possible Approaches Forward

1. **Registration-time detection** — check if WASM services (e.g. `WebAssemblyComponentsEndpointOptions`) are registered in the service collection. If WASM detected, register the decorator. Otherwise skip it.
2. **Avoid decoration entirely** — find a way to enrich claims in WASM without decorating `AuthenticationStateProvider`. Perhaps a separate service that components query directly.
3. **Fix the decoration pattern** — investigate exactly why the decorator breaks SSR. May require a minimal reproduction and deeper debugging of the Blazor Server rendering pipeline.

## Prerequisites
- **Three sample apps** for testing:
  - `Tharga.Platform.Sample` (existing) — Blazor Server
  - `Tharga.Platform.Sample.Wasm` — Standalone WASM
  - `Tharga.Platform.Sample.Hybrid` — Server + WASM (hybrid)
- **Integration tests** that confirm:
  - Server app renders pages with enriched claims
  - WASM app enriches claims client-side
  - Hybrid app works with both render modes
  - No SSR deadlock in any configuration

## Acceptance Criteria
- [ ] No manual `SkipAuthStateDecoration` configuration needed
- [ ] Works for Server, SSR, WASM, and Hybrid without configuration
- [ ] No SSR deadlock
- [ ] Verified with Server, WASM, and Hybrid sample apps
- [ ] Integration tests cover all hosting models

# Feature: Auto-detect Blazor Hosting Model

## Goal
Remove the need for `SkipAuthStateDecoration` by automatically detecting whether the app uses Server, WASM, or Hybrid hosting.

## Background
Currently `SkipAuthStateDecoration` defaults to `true` (server-side enrichment only). Standalone WASM apps must explicitly set it to `false`. Auto-detection would make it zero-config for all hosting models.

## Possible Approaches
1. **Registration-time detection** — check if WASM services are registered in the service collection
2. **Runtime detection** — make the auth state provider check `IJSRuntime.IsInitialized` or `IHttpContextAccessor.HttpContext` at call time (attempted but `IsInitialized` is protected and the decorator pattern breaks SSR)
3. **Enum-based** — replace bool with `BlazorHostingModel { Server, WebAssembly, Hybrid }` for explicit configuration (clearer API but still manual)

## Blockers
- The `AuthenticationStateProvider` decoration pattern causes silent deadlock during SSR prerendering
- Need a reliable way to skip JS interop during SSR without breaking the decorator chain
- Requires a WASM sample app to verify the client-side path

## Prerequisites
- WASM sample app for testing
- Integration tests for both hosting models

## Acceptance Criteria
- [ ] No manual `SkipAuthStateDecoration` configuration needed
- [ ] Works for Server, SSR, WASM, and Hybrid without configuration
- [ ] No SSR deadlock
- [ ] Verified with both Server and WASM sample apps

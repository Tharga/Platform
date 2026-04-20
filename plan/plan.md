# Plan: team-blazor-polish

## Steps

### Part A: IHostApplicationBuilder overload
1. [x] Add `AddThargaTeamBlazor(this IHostApplicationBuilder, Action<ThargaBlazorOptions>? options = null)` in `ThargaBlazorRegistration` — delegates to existing overload, passing `builder.Configuration`
2. [x] Unit test for the new overload — 3 tests (registration parity, configuration passthrough, null guard)

### Part B: Loading state in ApiKeyView
3. [x] `ApiKeyView`: add `_teamLoaded` flag, render `<Loading />` when not yet loaded, "No team selected." only after load completes with null team
4. [x] `TeamComponent`: verified — no bug. `_teams == null → <Loading />` already handles the pre-load state; there is no "No team selected" literal.
5. [x] `TeamSelector`: verified — already correct, no change

### Part C: Fix AuditLogView label
6. [x] Change `AuditLogView.razor` access-denied text from "No team selected." to "Access denied."

### Docs + close
7. [x] Run full test suite — 224 tests pass
8. [ ] Archive plan, delete plan/, final commit, push, PR

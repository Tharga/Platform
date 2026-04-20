# Feature: team-blazor-polish

**Originating branch:** master
**Date started:** 2026-04-20

## Goal

Two small quality-of-life improvements to `Tharga.Team.Blazor`:

1. `AddThargaTeamBlazor` — add an `IHostApplicationBuilder` overload so consumers don't need to pass `IConfiguration` separately. The builder already has both `.Services` and `.Configuration`.
2. `ApiKeyView` / `TeamComponent` — show a loading indicator instead of "No team selected." while the team-state task is in flight. Only display the "No team selected." message when the resolved team is genuinely null.

Both requests come from Florida / Quilt4Net Server internal use.

## Scope

### Part A: IHostApplicationBuilder overload

- New `AddThargaTeamBlazor(this IHostApplicationBuilder builder, Action<ThargaBlazorOptions>? options = null)` that delegates to the existing `IServiceCollection` overload with `builder.Configuration` threaded through automatically
- Keep the existing `IServiceCollection` overload unchanged (for test/non-builder scenarios)
- Consumers can replace `builder.Services.AddThargaTeamBlazor(o => ..., builder.Configuration)` with `builder.AddThargaTeamBlazor(o => ...)`

**Out of scope:** the builder-object refactor (`AddAuthentication().AddJwtBearer()` style). That's a larger API change and still called out as "consider" in the request. Leave for a follow-up if ever needed.

### Part B: Loading state in ApiKeyView and TeamComponent

- Track a tri-state: not-loaded / loaded-with-team / loaded-no-team
- While the team-state lookup is in flight, render `<Loading />` (same pattern as the existing `<Loading />` during keys fetch)
- Show "No team selected." only after the load completes and the team is null
- Apply to both `ApiKeyView.razor` and `TeamComponent.razor`
- `TeamSelector.razor` already has correct tri-state handling — verify no change needed

### Part C: Wrong label in AuditLogView

`AuditLogView` currently renders "No team selected." when `!_hasAccess`. That branch fires for access-denied (not a team-state race — `_hasAccess` resolves synchronously from claims). The text is misleading. Change it to "Access denied." to match the pattern used by `ApiKeyView`.

## Acceptance criteria

- [ ] New `AddThargaTeamBlazor(IHostApplicationBuilder, ...)` overload compiles and routes correctly
- [ ] Existing `IServiceCollection` overload unchanged
- [ ] `ApiKeyView` renders `<Loading />` before the team-state task resolves; shows "No team selected." only when the resolved team is null
- [ ] `TeamComponent` has the same behavior
- [ ] `AuditLogView` access-denied branch shows "Access denied." instead of "No team selected."
- [ ] All existing tests pass
- [ ] New tests cover the tri-state UI path and the new overload

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete.

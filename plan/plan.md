# Plan: LoginDisplay — gate the Team menu item by role (Tharga/Platform#100)

- [x] 1. Dependency bumps (patch) on Tharga.Team — verify build + tests
       Components.Authorization net9 9.0.16→9.0.17, net10 10.0.8→10.0.9; DI + DI.Abstractions
       10.0.8→10.0.9; Tharga.Toolkit 1.15.24→1.15.25. (MongoDB 2.11.0 deferred — separate follow-up.)
       Done: build green, 130/130 Blazor tests pass.
- [x] 2. Implement `TeamMenuRoles` + decision logic in LoginDisplay.razor
       Done: `string[] TeamMenuRoles` param + pure `ShouldShowTeamMenuItem`; any-of role match; blanks ignored.
- [x] 3. Tests: parameter default + decision matrix
       Done: LoginDisplayTests — 10 tests (1 param + 9 matrix), all pass.
- [x] 4. Docs: implementation-guide.md LoginDisplay row + usage note
       Done: table row + "restrict by role" usage example + note to also `[Authorize]` the page.
- [~] 5. Final build + full test run; summarize for user testing

## Notes
- Stashed change: `.claude/mission.md` Eplicta path fix
  (`$DEV_ROOT/Eplicta/plan/requests.md` → `$DOC_ROOT/Eplicta/requests.md`) — in git stash,
  handle separately (NOT part of this feature).
- `InternalsVisibleTo Tharga.Team.Blazor.Tests` already set on Tharga.Team.Blazor.csproj →
  the decision method can be `internal static` and tested directly.

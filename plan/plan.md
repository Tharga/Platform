# Plan: Host override for the "Create team" action (#123)

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

## Steps

- [x] **1. NuGet update check (mandatory, up front).** `dotnet outdated` across the whole
      solution. — *No outdated dependencies detected (2026-07-10). No upgrades to apply.*
- [x] **2. Baseline build + test.** `dotnet build -c Release` then `dotnet test -c Release`
      to confirm a green starting point before any feature code. — *Build clean (pre-existing
      warnings only); 550 tests green (185+9+51+305).*
- [x] **3. `ThargaBlazorOptions.CreateTeamPath`.** Added `public string CreateTeamPath { get; set; }`
      with XML doc explaining redirect + that `CreateTeamRequested` takes precedence.
- [x] **4. `TeamSelector` override.** Added `[Parameter] EventCallback CreateTeamRequested`;
      injected `IOptions<ThargaBlazorOptions>`; teamless "Create team" now: callback → anchor
      (`rz-link`, preventDefault) invoking it; else `RadzenLink Path=_createTeamPath`
      (`CreateTeamPath` ?? `/team`).
- [x] **5. `TeamComponent` override.** Added `[Parameter] EventCallback CreateTeamRequested`;
      read `_createTeamPath` from `BlazorOptions`; `CreateTeam()` precedence: callback →
      `NavigationManager.NavigateTo(path)` → `CreateTeamAsync()` + `ReloadTeams()`.
      `AllowTeamCreation` button-visibility gate preserved. Blazor project builds clean.
- [x] **6. Tests.** Added `CreateTeamOverrideTests` (reflection param/option checks + a `[Theory]`
      over the precedence resolver). Extracted precedence into `internal CreateTeamActionResolver`
      (mirrors `LoginDisplay.ShouldShowTeamMenuItem`) so the decision is unit-testable without bUnit.
- [x] **7. Verify.** Full solution green: 559 tests (was 550; +9). Build clean (pre-existing warnings only).
- [x] **8. Docs (`docs:` commit).** Added an "Overriding the Create team action" section to
      `Tharga.Team.Blazor/README.md`; added `o.CreateTeamPath` to the options example, a
      `CreateTeamRequested` row to the component-parameter table, and an override subsection in
      `docs/articles/implementation-guide.md`. `getting-started.md` needed no change (component
      list only).
- [x] **9. Sample (optional) — skipped.** Wiring `CreateTeamRequested`/`CreateTeamPath` into the
      sample would replace its real create flow with a stub, degrading the sample. Docs cover usage.
- [ ] **10. Close-out (only on user confirmation).** Re-run `dotnet outdated`; archive
      `plan/feature.md` to the external `done/`; `git rm -r plan`; final commit
      `feat: create-team-override complete`; push; open PR to `master`.

## Notes / decisions

- **Design source:** issue #123 offered 3 alternatives; user chose path + callbacks with
  precedence (callback > path > built-in). Post-create `TeamCreated` event explicitly
  declined (host owns onboarding on its own page/callback).
- **Branching:** GitHub Actions CI → branched from `master` (clean, up to date with origin).
- **Version:** additive/non-breaking → suggest 3.2.0 minor bump; CI-driven.

## Last session

2026-07-10 — Implementation complete (steps 1-9). Two commits on the branch:
`feat: host override for the Create team action (#123)` and
`docs: document Create team override (...)`. Full suite green (559 tests).
**Awaiting:** explicit approval to push the branch, then user testing from origin.
**Next (step 10, only after user confirms done):** re-run `dotnet outdated`, archive
`plan/feature.md` → external `done/`, `git rm -r plan`, final commit
`feat: create-team-override complete`, push, open PR to `master`.

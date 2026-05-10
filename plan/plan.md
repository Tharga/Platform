# Plan: TeamMember duplicate-row resilience

Branch: `feature/teammember-duplicate-fix` (created from `master`).

## Steps

- [x] **1. Confirm plan with user.** Done.
- [x] **2. Add the resolver helper.** `Tharga.Team.Blazor/Features/Team/TeamMemberResolver.cs` — internal static `Resolve<TMember>(...)` per spec.
- [x] **3. Add unit tests.** `Tharga.Team.Blazor.Tests/TeamMemberResolverTests.cs` — 5 tests (no match, single, two, three, null logger). All passing.
- [x] **4. Wire logger into TeamComponent.** `@inject ILogger<TeamComponent<TMember>> Logger` + `Microsoft.Extensions.Logging` `@using` added.
- [x] **5. Replace `.Single(...)` call sites.** All 5 sites in `TeamComponent.razor` converted. Action handlers fall through `NotifyMemberNotFound()` (extracted to avoid repeating the Radzen notify 4×); `HasAccessLevel` returns `false` on null.
- [x] **6. Build and run tests.** Build clean (0 warnings, 0 errors). All 268 tests passing (37 + 139 + 92 — was 263 + 5 new resolver tests).
- [~] **7. Manual sanity check.** Cannot reproduce duplicate-row scenario locally without seeded data. Resolver covered by unit tests; happy paths exercised by existing component tests.
- [x] **8. Commit.** Single commit including `plan/`. Two side-bugs filed to `Requests.md` instead of bundled here: (a) NRE on unauthenticated `GetTeamsAsync`, (b) inline name-edit broken for invited members (null `Member.Key`).
- [ ] **9. Hand back to user for testing.**
- [ ] **10. (After user OK) Open PR.** Push and open PR against `master`. PR description = release notes.
- [ ] **11. (After PR merged) Close out.** Archive `plan/feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/teammember-duplicate-fix.md`, delete `plan/`, final commit `feat: teammember-duplicate-fix complete`.

## Notes

- The `Tharga.MongoDB 2.10.9 → 2.10.10` and `Tharga.Blazor 2.1.4 → 2.1.5` upgrades shipped separately on `feature/upgrade-tharga-packages` (PR #60, merged before this branch was rebased).
- No README change planned — fix is internal resilience.
- A repository-layer unique index would prevent future duplicates but is explicitly out of scope (see `feature.md`).

## Last session (2026-05-10)

- Upgrades landed first via PR #60 (`Tharga.Blazor` → 2.1.5, `Tharga.MongoDB` → 2.10.10).
- This branch rebased on the new master; steps 2–6 executed in one pass.
- Bonus: helper handles `logger == null` defensively (covered by a 5th unit test).
- `NotifyMemberNotFound()` extracted to share between the 4 action handlers.
- Pending user sign-off before commit / push / PR.

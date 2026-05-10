# Plan: Unauthenticated TeamServiceBase guards

Branch: `feature/unauthenticated-team-service-guards` (already carries the close-out commit `fa36d13 feat: invited-member-edit-fix complete` from the previous feature — bundled per user preference).

## Steps

- [x] **1. Confirm plan with user.** Done.
- [x] **2. Add `RequireCurrentUserAsync` helper.** Added below the existing `GetCurrentUserAsync()` private helper.
- [x] **3. Refactor 7 call sites** in `TeamServiceBase`. All 7 done per the table.
- [x] **4. Defensive null guard at MongoDB boundary.** `TeamServiceRepositoryBase.GetTeamsAsync(IUser user)` now returns `AsyncEnumerable.Empty<ITeam>()` for null input.
- [x] **5. Tests.** `Tharga.Team.Service.Tests/UnauthenticatedTeamServiceTests.cs` with 6 tests, all passing. Note: `RemoveMemberAsync_Unauthenticated_Throws` had to target a non-owner member (user-2) because the owner-protection check (`The owner cannot leave the team. Transfer ownership first.`) fires before the `RequireCurrentUserAsync()` guard for the owner row.
- [x] **6. Build and run all tests.** Build clean (0 warnings, 0 errors). 280 / 280 tests passing (was 274, +6 new). 151 Service + 37 Mcp + 92 Blazor.
- [~] **7. Manual verification on sample.** Load a `<TeamComponent />` page without signing in (incognito or new browser session). Confirm:
    - No `NullReferenceException` in the host console.
    - The page renders the empty state ("You are not member of a team.").
    - Sign in and confirm the authenticated flow is unchanged.
- [ ] **8. Commit.** Conventional commit `fix: guard TeamServiceBase against null current user`. Includes `plan/`.
- [ ] **9. Hand back to user for testing.**
- [ ] **10. (After user OK) Open PR.** Push and open PR against `master`. Description doubles as release notes.
- [ ] **11. (After PR merged) Close out.** Archive `plan/feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/unauthenticated-team-service-guards.md`, delete `plan/`, final commit `feat: unauthenticated-team-service-guards complete`.

## Notes

- No NuGet upgrades needed (`dotnet outdated` reports nothing).
- The branch already carries the close-out commit for the previous feature (`fa36d13`). Single PR will include the previous `plan/` deletion + new `plan/` + this fix's code changes. Net result on master: previous `plan/` removed and new `plan/` added (this feature's, to be removed by its own close-out next).
- Marks the `UnauthorizedAccessException` choice: `System.UnauthorizedAccessException` (not a custom Tharga exception) keeps it standard for ASP.NET pipeline handling and aligns with the request's suggestion. Surfacing-as-401 is up to the consumer's pipeline.
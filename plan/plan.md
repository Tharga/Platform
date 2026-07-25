# Plan: Team member visibility without `users:manage`

Branch: `feature/team-member-visibility` (from `master`)

## Steps

- [x] 1. NuGet package update pass (mandatory feature-start step)
  - `dotnet outdated` across the solution reports one update: `SixLabors.ImageSharp` 3.1.12 → 4.0.0
    in `Tharga.Team.Images`. **Deliberately not applied** — ImageSharp 4.0 requires a paid Six Labors
    build-time license. The hold is pre-existing and intentional.
  - Baseline verified before any feature code: `dotnet build -c Release` succeeded,
    `dotnet test -c Release` green at 868 passed / 0 failed.

- [x] 2. `IUserService.GetTeamMemberUsersAsync()`
  - Added with XML docs stating the visibility rule and why the identity data cannot come from
    `ITeamMember`. Default interface implementation returns an empty list — additive, non-breaking.

- [x] 3. Implement it in `AuthorizationUserServiceDecorator`
  - Constructor takes an optional `Func<ITeamService>` (lazy — avoids the `ITeamService → IUserService`
    cycle; optional so existing two-arg construction, including in tests, still compiles).
  - Unions member keys from `GetTeamsAsync()` + `GetMembersAsync(key)` (both pass-through, no scope)
    and filters the undecorated `_inner.GetAsync()` by that set. Caller is always included.
  - 6 tests added: co-members returned, non-co-members excluded, spans every team, no teams →
    caller only, anonymous rejected before the store is touched, no factory → caller only.

- [x] 4. Wire the lazy `ITeamService` in `ThargaBlazorRegistration.DecorateUserServiceWithAuthorization`
  - Passed `sp.GetRequiredService<ITeamService>` as a method group.

- [x] 5. `UserDirectoryGate` for the component's load decision
  - `UserDirectorySource` enum + `Resolve(bool)`, next to `UserAdminGate`. 2 tests.

- [x] 6. `TeamComponent.OnInitializedAsync`
  - `_user` now loads before `_users`; the load branches on the gate. This is the line the reported
    stack trace terminated at (`TeamComponent.razor:358`).

- [x] 7. `TeamActionGate.CanEditConsent` — require the selected, managed team (#140)
  - Now `CanEditConsent(hasManageScope, selectedTeamKey, teamKey, isAdministrator)` composing
    `CanManage(...) && isAdministrator`, mirroring `CanDelete`. Call site updated. Theory extended
    to 5 cases covering non-selected team, missing scope, non-admin and no selection.

- [x] 7b. End-to-end wiring test (added beyond the original plan)
  - `TeamMemberUserWiringTests` resolves `IUserService` from a real container and asserts the
    co-member projection. The DI cycle is the live risk in this design and a lazy factory hides it
    from unit tests — only a real provider proves it. 4 tests, all pass.
  - Suite: 883 passed / 0 failed (baseline 868).

- [x] 7c. Sample app: Serilog file logging (requested mid-feature, not part of the fix)
  - `Serilog.AspNetCore` 10.0.0, console + daily rolling file to `<contentRoot>/logs/sample-.log`,
    14 files retained, `UseSerilogRequestLogging`. Levels moved from `Logging:LogLevel` to a
    `Serilog:MinimumLevel` section in both appsettings files (the old section is inert once Serilog
    is the provider). `logs/` added to `.gitignore`.
  - `ContentRootPath` rather than the working directory, so the folder is the project's own whether
    started from the IDE or `dotnet run`.
  - Verified by running the app: it wrote the Kestrel startup failure, with stack trace, to
    `logs/sample-20260725.log`.
  - Committed separately as `chore:` — unrelated to the two issues, so it stays out of the fix commit.

- [ ] 8. Verify in the sample app
  - Sign in as a user without a system role, confirm the page renders, create a team, confirm the
    owner row shows email and avatar, and that the consent drop-down is read-only on unmanaged teams.

- [ ] 9. Full suite + docs review
  - `dotnet build -c Release` and `dotnet test -c Release`.
  - Review `README.md` and `docs/` for the user-visibility rule — this changes what an ordinary
    member can see, so it likely warrants documenting rather than being a silent bug fix.

- [ ] 10. Close out
  - Re-run `dotnet outdated`, archive `plan/feature.md`, remove `plan/`, final commit, push, PR.

## Notes

- The issue text for #140 attributes the bug to `HasAccessLevel` returning a global access level.
  That is not the case — it resolves the member inside the given team and compares per-team.
  The real cause is that `team:manage` is issued for the selected team only. Worth a line in the
  PR description so the issue's stated diagnosis is not carried forward.

## Last session

2026-07-25 — Steps 1-7 complete, plus an unplanned end-to-end wiring test (7b). Both issues are
implemented and the suite is green at 883 (baseline 868). The reported stack trace confirmed
`TeamComponent.razor:358` as the sole blocker on the render path, exactly as traced statically.

Next: step 8 — verify in the sample app as a user without a system role (page renders, first team
can be created, owner row shows email/avatar, consent read-only on unmanaged teams). Then the docs
review in step 9: this changes what an ordinary member can see, so it likely warrants documenting
rather than shipping as a silent fix.

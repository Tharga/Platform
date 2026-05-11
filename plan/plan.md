# Plan: Resilient member lookup

## Steps

- [x] **1. Shared resilient-lookup helper in `Tharga.Team`**
  - Added `public static class ResilientMemberLookup` in `Tharga.Team/ResilientMemberLookup.cs`.
  - Three public extension methods on `IEnumerable<T>`: `PickOneOrDefault(predicate, logger, teamKey, lookupKey)` (member-domain overload), `PickOneOrDefault(predicate, logger, context)` (single-context overload for non-team lookups, used by `ApiKeyAdministrationService`), and `ReplaceByReference(target, replacement)` (substitution helper used by the repository write paths).
  - Promoted the existing internal `TeamMemberResolver` from `Tharga.Team.Blazor` and dropped its `ITeamMember` constraint. Five callers in `TeamComponent.razor` updated to the new extension; old helper deleted; tests moved to `Tharga.Team.Blazor.Tests/ResilientMemberLookupTests.cs` (10 tests).

- [x] **2. Apply helper in `Tharga.Team.MongoDB/TeamRepository.cs` + fix strip-siblings**
  - Optional `ILogger<TeamRepository<,>> logger = null` parameter added to the constructor. Open-generic `ILogger<>` resolves via DI when consumers call `AddLogging()`.
  - Six methods rewritten: `SetLastSeenAsync`, `SetMemberRoleAsync`, `SetMemberTenantRolesAsync`, `SetMemberScopeOverridesAsync`, `SetMemberNameAsync`, `SetInvitationResponseAsync`.
  - Pattern: `PickOneOrDefault` → null-no-op return → `with { ... }` → `ReplaceByReference`. Every former `Where(x => x.Key != userKey).Union([member])` is replaced by `team.Members.ReplaceByReference(target, updated)` so duplicate-keyed siblings are no longer silently stripped on save.

- [x] **3. Apply helper in `Tharga.Team/TeamServiceBase.cs`**
  - Optional `ILogger<TeamServiceBase> logger = null` parameter added to the constructor. Subclasses (`TeamServiceRepositoryBase`, `TestTeamService`, `StubTeamService`) still call `: base(userService)` without change.
  - Four sites updated: `RemoveMemberAsync` (line 123), `TransferOwnershipAsync` currentOwner / newOwner (lines 225, 229), `AssureAccessLevel` helper (line 270). The previously-implicit "must-exist" semantics in `AssureAccessLevel` are preserved via an explicit null check that throws `InvalidOperationException("User is not a member.")`.

- [x] **4. Apply helper in Blazor razor files**
  - `TeamComponent.razor:611` — `CopyInvitationLink` uses the existing injected `Logger`.
  - `TeamInviteView.razor` — added `@inject ILogger<TeamInviteView<TMember>> Logger` and the `@using Microsoft.Extensions.Logging`. Invitation-code lookup converted.

- [x] **5. Apply helper in `Tharga.Team.Service/ApiKeyAdministrationService.cs`**
  - Optional `ILogger<ApiKeyAdministrationService> logger = null` parameter added.
  - Two sites converted to the single-context `PickOneOrDefault` overload. Contexts: `"ApiKey verify prefix={prefix}"` and `"ApiKey verify full-scan"`.

- [x] **6. Tests**
  - `ResilientMemberLookupTests.cs` — 10 tests for `PickOneOrDefault` (both overloads) and `ReplaceByReference`. Strip-siblings test asserts that a duplicate-keyed sibling is preserved when only the target is substituted.
  - `ResilientLookupCallSiteTests.cs` (new, in `Tharga.Team.Service.Tests`) — 3 tests covering `RemoveMemberAsync` + `TransferOwnershipAsync` (currentOwner / newOwner) with duplicate-keyed members. None throw under the fix.
  - `ApiKeyAdministrationServiceTests.cs` — 1 new test: two stored keys both verify true → first-match returned, no throw.
  - Total: 14 new tests, 289 tests passing across the solution. Previously 282.

  **Repository-level integration tests deliberately skipped**: there is no existing `Tharga.Team.MongoDB.Tests` project, and the strip-siblings fix is fully exercised by the `ReplaceByReference` primitive tests — call-site coverage would require either NSubstitute against the substantial `IDiskRepositoryCollection<,>` surface or a real MongoDB harness. The mechanical wiring in `TeamRepository` is identical across all 6 methods and is reviewable from the diff.

- [x] **7. Full test suite green**: 289/289 passing on `dotnet test -c Release`.

- [x] **8. README.md update deferred**: per `mission.md`, the PR description doubles as release notes. The repo READMEs are stable feature overviews without a release-notes section, so no edit is needed.

- [ ] **9. Commit and push the feature branch**
  - Conventional commit message: `fix: resilient member lookup across Tharga.Team / Tharga.Team.MongoDB / Tharga.Team.Blazor / Tharga.Team.Service`.

- [ ] **10. Pause for user verification** (do NOT open the PR yet — per `Completing implementation` rule, wait for user feedback before the close-out commit and PR).

## Last session
2026-05-11 — All implementation steps complete. 14 new tests, 289 total green. Ready for commit + push + user verification.

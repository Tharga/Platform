# Plan: Teams-tab action templates

Feature scope in `feature.md`. Tests run before each commit; `plan.md` is updated as each step lands.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      *Done — `dotnet outdated` across the whole solution reports only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, which is deliberately held: 4.0+ requires a paid Six Labors build-time licence.
      Nothing to apply.*

- [x] **2. Tests first — the wrapper forwards both templates**
      *Done — `UsersViewTemplateForwardingTests`, 5 tests, all red for the right reason (the parameters do
      not exist yet). Follows the `AdminViewSurfaceTests` reflection precedent.*
      Beyond the two obvious cases, added `WrapperTemplateType_MatchesTheChild` (wrapper and child must
      agree on the type, or a host writes its template against a shape that never renders) and
      `EveryChildActionTemplate_IsReachableFromTheWrapper` — a standing guard that fails if any future
      child gains a `*Template` parameter the wrapper does not forward, which is the class of omission
      this whole feature exists to fix.
      **Known limitation, recorded in the test file's `<remarks>`:** this asserts the parameter *surface*,
      not the *wiring*. Deleting the pass-through from the markup while leaving the property declared
      would still pass. This is the first test that should move to bUnit.

- [x] **3. Forward the templates in `UsersView.razor`**
      *Done — `TeamActionsTemplate` and `MemberActionsTemplate` added as `[Parameter]`s and forwarded on
      the `<TeamsListView>` element. XML docs mirror the users-side wording and name the problem the
      Teams-tab template solves, so the reason it exists is discoverable from IntelliSense.*

- [x] **4. Verify build + full suite** — *Done: 1025 passed / 0 failed across 6 projects; build clean.*

- [x] **5. Document the admin-surface scopes end to end**
      *Done — new `## Which scope gates what` section in `user-management.md` carrying the matrix, the
      system-grant rule, the two routes to `teams:read`, a `### ConfigureSystemScopes does not withhold
      these from API keys` subsection, and a `### A known asymmetry: deleting users` subsection.
      `## Deleting users` now states its scope up front; `## Deleting teams` gained the `teams:read`
      visibility point and now says plainly that deletion cannot be undone (no soft delete today).
      `implementation-guide.md` got the `ConfigureSystemScopes` correction as a blockquote where that
      option is described. Blazor README got the three-scope summary, the user-delete asymmetry, the
      registry note, and — since it was the one place listing component parameters — the row-action
      template set including the two this feature adds.*
      Widened on request: not just the `ConfigureSystemScopes` correction, but a clear statement of which
      system scope gates which action on `<UsersView />`. Both surfaces —
      `docs/articles/user-management.md` (the reader-facing home) and `Tharga.Team.Blazor/README.md`, with
      the `ConfigureSystemScopes` correction also in `docs/articles/implementation-guide.md` where that
      option is described. Separate `docs:` commit.

      A table, since the three-scope answer for team delete is not guessable:

      | Action on `/…/user` | Scope(s) required | Notes |
      |---|---|---|
      | See either tab at all | `users:manage` | Without it the view loads no data |
      | See teams you are not a member of | `teams:read` | Else only your own teams are listed |
      | Delete a team | `users:manage` + `teams:read` + `teams:delete` | `teams:read` is not *required* to delete, but without it the cross-team targets are invisible |
      | Delete a user | `users:manage` | No separate scope — see the asymmetry note |
      | Verify / directory-only | `users:manage` + a registered `IUserDirectoryService` | Hidden entirely when unregistered |
      | Audit-log dialog | `ShowAuditLogButton` + `audit:read` | Opt-in parameter |

      Must also state: (a) all of these must be **system** grants — an in-team claim of the same name never
      satisfies them; (b) `teams:read` may arrive via `o.Blazor.Consent.GrantTeamsRead` *or* a direct
      `ConfigureSystemRoles` mapping, which is why it is absent from the sample's `Map("Developer", …)`
      list; (c) `ConfigureSystemScopes` does **not** withhold `teams:delete` / `users:manage` from system
      API keys — the framework auto-registers both; (d) the `users:manage`-alone asymmetry for user delete.

- [x] **6. Push and hand over for testing**
      *Pushed 2026-07-30. PR deliberately not opened — the close-out commit must be last.*

- [x] **7. Fold in testing feedback** (scope added 2026-07-30 after the user tested `/users`)
      *Done — see `feature.md` §3. Identity + directory id in the user detail panel (new pure
      `DirectoryLink` helper, 6 tests); Teams row actions converted to a split button with
      `TeamActionItems` / `TeamActionInvoked` / `TeamRowAction` for parity with the Users tab; the
      sample's overlapping "Edit" narrowed to "Edit email". 1034 tests green.*
      **Filed to the backlog rather than built:** per-user audit tracking — a stable `CallerUserKey` on
      `AuditEntry`, a caller filter in `AuditLogView`, a team-admin per-member audit link, and a target
      subject for "what was done *to* a user". Recorded under `Toolkit/Team.md` → Audit with what already
      works, since most of it turned out to be UI rather than missing recording.

- [~] **8. Re-push and hand over for re-testing**

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`; mark the PlutusWave request **Done** in `Requests.md` with the three-ask
breakdown and add the `## Follow-up` entry; archive `feature.md` to
`$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/`; `git rm -r plan`; commit
`feat: teams-tab-action-templates complete`; push; open the PR.

## Notes

- **No version-line change.** This is additive-but-patch-sized on the 3.7 line; `MAJOR_MINOR` stays `3.7`
  and CI increments the patch from git tags. Contrast 3.7.0, which needed a hand-edited version line
  because it grew public API.
- **The docs fix is deliberately not a code change.** Narrowing the system scope registry to reflect only
  what the host registered would silently break anyone relying on today's auto-registration, and the admin
  surfaces need those scopes grantable. Document the behaviour; revisit only if a consumer needs real
  isolation.

## Last session

Branch created off master after 3.7.0 (PR #162) merged. Feature scoped to the two open asks of
PlutusWave's 2026-07-30 `UsersView` request; its ask #2 was already delivered by 3.7.0.

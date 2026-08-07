# Plan: role badges on the profile page (#155)

## Steps

- [x] 1. NuGet check (mandatory, up front). Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held for its paid
      build-time licence. Nothing to apply.

- [x] 2. Extract `TeamRoleNames.IsTeamDerived` and point `TeamClaimRevalidator.IsTeamRole` at it, so the
      profile page does not become a second copy of the rule.

- [x] 3. `ProfileRoles.Read(principal)` → `ProfileRoleSet(App, Team)`. Pure, so the classification is asserted
      by tests rather than living in markup.

- [x] 4. Markup in `UserProfileView.razor`: a wrapped badge row in the identity card, app roles in flat
      `Secondary` (matching `ScopeView`), team roles in `Info` with a tooltip, plus a caption when any team
      role is present. Claims card untouched.

- [x] 5. Tests — `ProfileRolesTests`, 14 cases. The split; every `AccessLevel` classified as team-derived;
      `TeamLead` / `Teamster` / `Team` staying app roles; duplicates collapsed; stable order; no roles; null
      principal; empty role value; non-role claims ignored.

- [x] 6. Build + full suite: **1845 pass**, 0 errors, warnings at the **11** baseline.

- [ ] 7. Close-out: archive `feature.md` to the Plan directory `done/`, `git rm -r plan`, final commit, push,
      open the PR. **Only when the user confirms.**

## Notes / decisions

- **Split, not one flat row.** #155 offered either and said flat was acceptable as a first pass. Split chosen
  because team roles change with the selector — reasoning in `feature.md`.
- **No documentation change.** `/profile` is a component a host places on its own page; the guide documents the
  component list, not its visual content, and nothing about configuration or wiring changed. Reviewed and
  deliberately skipped rather than overlooked.
- The `TeamLead` / `Teamster` tests exist because the predicate this replaced tested the `"Team"` prefix before
  matching an access level; those cases guard the intent rather than the old shape.

## Last session

Steps 1–6 complete. Nothing pushed, no PR.

**Carried forward — §3b needs a decision before it can be built.** The spec's premise is partly stale: its
example (`IApiKeyManagementService`) now uses `AddTeamService`, but `ITeamManagementService` and its facets are
registered with plain `TryAddScoped`, deliberately — `TeamManagementService` enforces reads by hand with
`RequireTeamReadAsync` and its own docs state the `[RequireScope]` attributes on that interface are
documentation only. So §3b as written would fail the toolkit's own registration. It needs a way for a
self-enforcing service to declare itself. Worth doing: right now a developer adding a read plus the attribute
gets no enforcement, and only a comment says so.

Also still open: plan 01 §6b, GitHub #142, the four Easy items, and the stale release runs for #198/#199/#200
which should be **cancelled** rather than approved.

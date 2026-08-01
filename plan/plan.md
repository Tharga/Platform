# Plan: Move every first-level surface onto the gated read path

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-01, before the branch was cut.** Unchanged: `SixLabors.ImageSharp`
      3.1.12 → 4.0.0 held for the paid-licence reason. Everything else current.

---

## 2. Close the gap PR 1 missed  `[~]`

- [ ] `GetTeamCustomRolesAsync` on `ITeamManagementService`, `[RequireScope(TeamScopes.Read)]` — reading
      a team's custom roles is reading team detail. Implement in `TeamManagementService` **through
      `RequireTeamReadAsync`**, not as a pass-through: a pass-through is exactly what PR 1 found and
      fixed on the other four.
- [ ] Test it alongside the existing four.

---

## 3. Move the surfaces

**One commit per surface or tight group, so a regression bisects to a single component.**

- [ ] `TeamDialog`, `InviteUserDialog` — one call each, both already gated. Cheapest first, and proves
      the injection swap is as mechanical as it looks.
- [ ] `AuditLogView.razor.cs` — `GetTeamsAsync` → `ITeamDirectoryService`.
- [ ] `TenantRoleManager` — depends on step 2.
- [ ] `UsersListView`, `TeamsListView` — reads plus the cross-team enumeration.
- [ ] `TeamSelector` — needs `TeamStateService` to bridge `SelectTeamEvent` first. **Check the bridge
      does not double-fire**: the selector already handles `SelectedTeamChangedEvent`, and setting
      `SelectedTeam` from the bridged event may raise it again.
- [ ] `TeamComponent` — the largest, four distinct calls.
- [ ] `TeamInviteView` — `GetInvitationAsync` replaces "read any team by key, match the code in memory".
      **This one changes what the screen can see**, so re-read the markup rather than swapping calls.
- [ ] MCP `TeamResourceProvider`, `TeamUserResourceProvider`.
- [ ] `AccessPage` in the sample — it is the worked example consumers copy, so it teaches whatever it
      does.

**After each: does a Viewer-level member still see exactly what they saw before?** Acceptance criterion 2
is the one that breaks silently, and every surface is a chance to break it.

---

## 4. Guards

- [ ] **Architecture test**: no component, dialog or MCP provider injects `ITeamService`. Must fail
      **naming the offending type** — a test that just says "something injects it" teaches nothing to the
      person who added the surface.
- [ ] Find surfaces **by type**, not by scanning `.razor` — that is how PR 1 missed three of them.
      Reflect over the assembly for `ComponentBase` subclasses and MCP provider implementations.
- [ ] `[EditorBrowsable(Never)]` on `ITeamService` + XML docs naming it the host's implementation
      contract, not for injection.

---

## 5. Verify the two paths a naive gate breaks

Not optional, and not covered by unit tests.

- [ ] **Sign-in**: claims construction reads team data before any scope exists. `TeamMembershipClaimsBuilder`
      and `TeamClaimsAuthenticationStateProvider` must stay on `ITeamService` — confirm they still do
      after the sweep, and that the architecture test does not flag them.
- [ ] **Invitation acceptance**: an invitee holds no scope for the team they are joining. Run it in the
      sample end to end.

---

## 6. Documentation

- [ ] Release note: **a caller lacking `team:read` starts being refused**, what to grant, and that
      `AccessLevel.Custom` is where it bites.
- [ ] Update the "Which team service to inject" tables added in PR 1 with `GetTeamCustomRolesAsync`.
- [ ] Separate `docs:` commit before close-out.

---

## 7. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings, **sample runs**.
- [ ] **`MAJOR_MINOR` → `3.10`.**
- [ ] Archive to `$DOC_ROOT/.../done/move-surfaces-to-gated-reads.md` and **move
      `07-move-surfaces-to-gated-reads.md` out of `planned/`** — this is the plan that finishes it.
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-01 (setup).** Branch cut off `master` after PR 1 merged. Package check unchanged. Plan written,
**awaiting confirmation before any code changes.**

**The survey correction worth remembering:** the surface count went 8 → 11 because the first pass grepped
`.razor` only. `AuditLogView.razor.cs`, `TenantRoleManager.razor` and `TeamStateService.cs` were invisible
to it — and the last of those is the one that should *stay*. Step 4 therefore finds surfaces by type
rather than by file extension, so the guard cannot inherit the same blind spot.

**Next:** confirm the plan, then step 2.

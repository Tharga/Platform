# Plan: Finding a team when there are many of them

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-03.** Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held because 4.0+
      enforces a paid build-time licence. Nothing to apply.

## 2. Version

- [ ] **Minor on `3.10`.** New parameters; the team list's presentation changes.

---

## Part A — the selector filter

## 3. The threshold

- [ ] A tested helper answering "is a filter worth showing for this many teams?", beside
      `TeamSelectorGate` and modelled on `AuditFilterVisibility`. Not an inline `Count > n`.
- [ ] Pick the number and say why. A dropdown of five is scanned faster than it is typed into; somewhere
      around eight to ten is where that flips.
- [ ] A parameter so a host can force it on or off, defaulting to the threshold.

## 4. The control

- [ ] `AllowFiltering` on the existing `RadzenDropDown`, with `AllowClear` where it helps.
- [ ] **Check `Template` / `ValueTemplate` still render while filtering** — the dropdown has both, and
      the templates run per keystroke and cannot await. `_suspendedTeamKeys` is already resolved ahead of
      render for exactly that reason; nothing new may depend on an await in a template.

---

## Part B — the team list as a grid

## 5. The grid

- [ ] `RadzenDataGrid` over `_teams`, replacing the `ExpandableCard` stack.
- [ ] **Read `AuditLogView` first and follow it** — paging, sorting, filter state and `PageSizeOptions`
      are already solved there, and a second pattern for the same job is worse than either.
- [ ] Columns: icon + name, the consent badge and "not a member" badge for an oversight caller, member
      count, team actions.
- [ ] Row detail = the existing members `RadzenDataGrid`, moved across as intact as possible.
- [ ] The selected team stays visibly distinct — `RowRender` with a class, replacing
      `ExpandableCard.Selected`.

## 6. Actions and gating

- [ ] Every team-level action still present: rename, delete, leave, consent, custom roles, icon, audit,
      transfer/assign owner.
- [ ] **Gating untouched.** `TeamActionGate`, `TeamVisibility` and every scope check keep their current
      inputs and meaning. This is a presentation change; if a diff touches an authorization decision,
      that is a mistake, not a refactor.
- [ ] Actions that opened a dialog keep opening the same dialog.

## 7. Tests

- [ ] The threshold helper, including both sides of it and the parameter override.
- [ ] Whatever decisions move out of markup get the same treatment as `TeamSelectorGate` and
      `TeamVisibility`: pure, static, tested. **There is no bUnit here, so a decision left in markup is
      untestable** — that constraint is what shaped those helpers and it has not changed.
- [ ] A test that the actions surface is unchanged in what it gates on, so the refactor cannot quietly
      widen access.
- [ ] Mutation-check each guard.

## 8. Documentation

- [ ] `Tharga.Team.Blazor/README.md` — the component summaries for `TeamComponent` and `TeamSelector`
      both describe today's presentation and will be wrong.
- [ ] `docs/` — check `implementation-guide.md` for the same.
- [ ] Separate `docs:` commit.

## 9. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 11).
- [ ] Archive `feature.md`, `git rm -r plan`, final commit, push, PR.

---

## Open, and worth deciding before it costs anything

- **Where the team-level actions live in a grid.** A column of buttons per row, an overflow menu per row,
  or inside the expanded detail. `ApiKeyView` settled the same question with a single `⋮` overflow menu
  and that is the precedent worth following — but a team has fewer, more prominent actions than an API
  key, so it is worth a look before copying.
- **Whether `TeamComponent` keeps its parameter surface.** It has several (`ShowRoles`,
  `ShowScopeOverrides`, `ShowScopeTooltip`, …) that belong to the *members* grid and should survive
  untouched. Any new ones are for the *teams* grid and should be named so the two are not confused.
- **Paging does not fix the load.** Recorded in `feature.md`. Worth a backlog entry for the paged team
  read rather than leaving the gap implicit.

## Last session

**2026-08-03 (planning).** Branch cut off `master` at 027056f, after #188 and #190 merged. Package check
unchanged.

**Two parts, very different sizes.** The selector filter is a parameter and a threshold helper. The team
list is a real refactor of a 1000-line component — but the members grid inside it already is a
`RadzenDataGrid`, so the work is the outer container and where the actions live, not the contents.

**Next:** confirm, settle the two open questions, then §3.

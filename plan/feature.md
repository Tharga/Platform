# Feature: Finding a team when there are many of them

**Branch:** `feature/team-list-and-selector` (off `master`)
**Started:** 2026-08-03
**Release:** **minor on `3.10`.** New parameters with defaults that preserve today's behaviour where it
still makes sense; the team list's presentation changes.

From the backlog (`$DOC_ROOT/Tharga/Toolkit/Team.md`), restated and expanded by the user 2026-08-03.
**Tier 3** — internal, no consumer filed it. UI only: no contract, no authorization, no persistence, so
it is neutral to v4.

## The problem

Both surfaces were built for a handful of teams and stop working past that.

- **`TeamComponent`** renders one `ExpandableCard` per team, stacked. Twenty teams is a page of
  accordions with no way to sort, filter or page.
- **`TeamSelector`** renders a `RadzenDropDown` with no filtering, so finding a team means scanning.

An oversight caller — anyone with `teams:read` — sees *every* team, which is exactly the population this
breaks for.

## Part A — a filter in the selector

Add `AllowFiltering` to the dropdown, **shown only once it earns its place**: hidden for a few teams,
present once scanning is the slow part.

The threshold belongs in a small tested helper, not an inline `Count > n` in markup.
`AuditFilterVisibility` already encodes the same judgement for the audit filter bar — *"one option is not
a filter"* — and this is the same call made about a different control. Following that gives it a home
where it can be reasoned about and changed once.

## Part B — the team list as a grid

Replace the stack of `ExpandableCard`s with a `RadzenDataGrid` whose **expanded row** shows that team's
members, with **sorting, filtering and paging** on the team list itself.

The inner members grid is already a `RadzenDataGrid`; it moves into the row detail template largely
unchanged. What changes is the outer container and where the team-level actions live.

**`AuditLogView` is the worked example** and should be followed rather than re-invented — it is this
codebase's existing `RadzenDataGrid` with paging, sorting and filter state, down to `PageSizeOptions`
and the "one option is not a filter" treatment.

## What paging here does and does not fix

**It is a rendering fix, not a loading one.** `ReloadTeams` fetches every team *with its members* eagerly
(`GetAllTeamsAsync<TMember>()`), so paging changes how much is drawn, not how much is fetched. On a large
tenant the fetch is the more expensive half.

Fixing that means a paged, member-less team read — a new service contract, which is a v4-shaped decision
(rule 3: contracts serialize, paged results carry an explicit cursor) and much larger than this. **Out of
scope, and recorded here rather than left as a surprise**, because a reader who sees "paging" will
otherwise assume the load was fixed too.

Part A has the same shape: a client-side filter needs every team loaded, which is the thing a paged read
would stop doing. Both parts are worth having anyway — they fix what a person experiences today — but
neither is the fix for a tenant with thousands of teams.

## Scope

1. `TeamSelector`: filtering, above a threshold, in a tested helper.
2. `TeamComponent`: teams as a `RadzenDataGrid` with expandable rows, sorting, filtering and paging.
3. Team-level actions and badges keep working, from wherever they end up living.
4. The selected team stays visibly distinct, as `ExpandableCard.Selected` does today.
5. Parameters to control the new behaviour, defaulting to what a small tenant already sees.

## Not in scope

- **A paged team read.** Above.
- **Changing what a caller may see.** Visibility is `teams:read` and stays exactly as it is; this is
  presentation only. Nothing here should touch `TeamVisibility`, `TeamActionGate` or any scope check.

## Acceptance criteria

1. The selector offers a filter above the threshold and not below it.
2. The team list sorts, filters and pages.
3. A team's members are reachable by expanding its row.
4. Every team-level action available today is still available, gated exactly as it is now.
5. The selected team is visibly distinct.
6. A small tenant sees no controls it does not need.
7. Full suite green; no new warnings.

## Done condition

All seven met, docs on both surfaces, `plan/` removed in the close-out commit, PR open against `master`.

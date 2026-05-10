# Feature: TeamMember duplicate-row resilience in TeamComponent

GitHub issue: [Tharga/Platform#59](https://github.com/Tharga/Platform/issues/59)

## Problem

When a `Team.Members` collection contains two or more rows matching the same predicate (typically same `Key` from a migration glitch or missing unique constraint), every `.Single(predicate)` call site in `TeamComponent<T>` throws `InvalidOperationException: Sequence contains more than one matching element`. The exception bricks the team-management page until reload — observed 5x in 24h on Quilt4Net.Server prod (consumer of `Tharga.Team.Blazor` 2.0.16).

## Goal

Make `TeamComponent<T>` resilient to duplicate `TeamMember` rows. The page must remain usable; duplicates are surfaced via warning logs so operators can clean them up.

## Scope

In-scope (5 call sites in [TeamComponent.razor](../Tharga.Team.Blazor/Features/Team/TeamComponent.razor)):

1. `RemoveUserFromTeam` (line 394) — the call site in the issue.
2. `ChangeRole` (line 406).
3. `ChangeTenantRoles` (line 316).
4. `ChangeScopeOverrides` (line 325).
5. `HasAccessLevel` (line 535) — runs every render; highest crash exposure.

Out of scope:
- Repository-layer unique index on `(TeamKey, UserKey)` — separate change, needs migration plan for existing duplicates.
- Self-healing delete-on-read — couples a read handler to a destructive write; risk too high for this fix.
- Other `.Single(...)` usages outside `TeamComponent.razor`.

## Approach

1. Inject `ILogger<TeamComponent<TMember>>` into the component.
2. Extract a small helper `ResolveMember(IEnumerable<TMember> members, string key, ILogger logger, string teamKey)` that:
   - Materializes matches once.
   - Returns `null` if none found.
   - Returns the first match if exactly one.
   - Logs a warning with team key + member key + match count if more than one, then returns the first match.
3. Replace all 5 `.Single(predicate)` sites with the helper.
4. Each call site handles a `null` return defensively (member already gone — no-op or graceful UI message).
5. Unit-test the helper against the four cases (none, one, two-or-more, null inputs).

## Acceptance criteria

- All 5 call sites use the new helper instead of `.Single(...)`.
- A team with duplicate member rows no longer crashes the page on any of the 5 actions.
- Warning log emitted (with team key + member key + count) when duplicates are encountered.
- New unit tests cover: no match, single match, two matches (warning logged + first returned), three matches.
- Existing tests still pass: `dotnet build -c Release && dotnet test -c Release`.
- README/docs unchanged (internal resilience fix; no public API change beyond a new helper that may stay `internal`).

## Done condition

User confirms the fix works against a reproduction (or against the production data that produced the original error).

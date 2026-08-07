# Feature: role badges on the profile page (#155)

## Goal

Show the caller's roles on `/profile` without making them expand a card and read schema URIs.

## Problem

The only way to see which roles you hold is to expand the **Claims** card — an `ExpandableCard` with
`Selected` unset, so it starts collapsed — and then read raw claim lines keyed by
`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. Roles are the thing people open this page to
check, and they are behind a click and buried in a flat list of every claim.

## Scope

Badges in the identity card at the top, always visible. **The Claims card is untouched** — this is an
addition, as the issue specified.

**Split into app roles and team roles**, which #155 explicitly left to the implementer. Reasoning:
`TeamMembershipClaimsBuilder` synthesises `TeamMember` and `Team{AccessLevel}` from whichever team is
selected, so those values change when the caller switches teams while app roles do not. One undifferentiated
row misrepresents both — a team role reads as a permanent grant, and an app role reads as something the team
selector might take away. Team roles get `BadgeStyle.Info` plus a tooltip and a one-line caption; app roles
keep the flat `Secondary` style `ScopeView` already uses, so a role looks the same wherever it appears.

## One thing this fixed on the way

**"Is this a team-derived role" existed only as a private predicate inside `TeamClaimRevalidator`.** Rather
than write a second copy for the profile page, it is extracted to `TeamRoleNames.IsTeamDerived` and the
revalidator now delegates to it. Two implementations of that rule would drift, and a role misclassified in one
place but not the other is the kind of difference nobody notices until it matters.

The classification itself lives in `ProfileRoles.Read`, pure and separately testable, rather than in markup
where no test could reach it.

## Acceptance criteria

- [x] Roles are visible on `/profile` without expanding anything.
- [x] App roles and team-derived roles are visually distinguishable, with the distinction explained on screen.
- [x] Every `AccessLevel` produces a team role, so a new level cannot land in the app column.
- [x] A role that merely *starts with* "Team" (`TeamLead`, `Teamster`) stays an app role.
- [x] Duplicates collapse, ordering is stable, non-role claims are ignored.
- [x] A null principal and an empty role value do not throw — the page renders before the principal resolves.
- [x] The Claims card is unchanged.
- [x] Full suite passes with no new warnings.

## Done condition

#155 closable: roles readable at a glance, with team-derived ones marked as following the selector.

## Out of scope

The issue's own possible follow-up — the same treatment for other high-signal claims (email verified, tenant,
scope) so the Claims card is only needed for debugging. Worth doing if this lands well; not started.

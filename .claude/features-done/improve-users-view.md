# Feature: Improve UsersView (Request #10)

## Goal
Redesign UsersView from a basic technical listing into a usable admin interface with tabs, filters, and team membership cross-reference.

## Scope

### Users tab
- Gravatar avatar, display name, email, last seen
- Filtering by name/email, sorting by columns
- Click user to see which teams they belong to and their role in each

### Teams tab
- Team name, icon, member count
- Click team to see its members

### Cross-reference
- Ability to navigate between user → teams and team → members

- From: Tharga.Platform (self-request) — Priority: Medium

## Acceptance Criteria
- [ ] Users tab with Gravatar, name, email, filters, sorting
- [ ] Teams tab with name, icon, member count
- [ ] Clicking a user shows their team memberships
- [ ] Clicking a team shows its members
- [ ] Developer role still required

# Feature: Team Consent for Developer/Admin Viewer Access

## Originating branch
develop

## Goal
Allow teams to grant consent for users with specific global roles to access the team as viewers.

## Approach
1. Add `ConsentedRoles` to ITeam/TeamEntityBase
2. Add SetTeamConsentAsync to service layer + repository
3. Extend GetTeamsAsync to also return consented teams for the user
4. Claims transformation: if not a member but has consent, add viewer-level claims
5. TeamComponent: consent toggle for admins
6. Config: ConsentRoles, ShowConsentToggle, DefaultConsent

## Done Condition
All acceptance criteria in the feature plan met.

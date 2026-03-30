# Feature: Document CreateTeam/CreateTeamMember Patterns (Request #4)

## Goal
Document the full pattern for overriding CreateTeam and CreateTeamMember, including Invitation and Name fields.

## Scope
- Add documented example to implementation-guide.md showing complete CreateTeamMember override
- Include Invitation object creation (InviteKey, EMail, InviteTime)
- Include Name field on member
- Consider whether Platform should auto-generate the Invitation object in AddMemberAsync
- **Reference implementation:** See `TeamService` in `Tharga.TemplateBlazor.Web` (`C:\dev\tharga\BlazorTemplate`) for a working example
- From: Eplicta.FortDocs — Priority: Medium

## Acceptance Criteria
- [ ] Implementation guide has complete CreateTeamMember example with Invitation
- [ ] Decision documented on whether Invitation should be auto-generated
- [ ] If auto-generation is chosen, implement and test it

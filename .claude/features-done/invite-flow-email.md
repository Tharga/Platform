# Feature: Invite Flow Email

## Goal
Improve team member invitation: make name mandatory, email optional, support email sending, always show copy-link.

## Originating branch
develop

## Scope
1. Create `ITeamEmailSender` interface in Tharga.Team
2. Create `SmtpTeamEmailSender` in Tharga.Team.Service
3. Create `EmailOptions` in Tharga.Team.Service
4. Add `AddEmailService<T>()` to `ThargaPlatformOptions`
5. Wire up registration: custom sender > SMTP (if EmailOptions set) > nothing
6. Update `InviteUserDialog`: name required, email optional, show invite link, send email if configured
7. Update `TeamComponent` invite flow to show link in dialog result
8. Tests

## Acceptance Criteria
- [ ] Name is required, email is optional in invite dialog
- [ ] Invite link is always visible for manual copying after invite
- [ ] When email provided and ITeamEmailSender configured, email is sent
- [ ] When ITeamEmailSender not configured, clear message shown
- [ ] AddEmailService<T>() works in ThargaPlatformOptions
- [ ] SmtpTeamEmailSender works with EmailOptions
- [ ] All tests pass

## Done Condition
All acceptance criteria met, user confirms done.

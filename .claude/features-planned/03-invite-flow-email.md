# Feature: Improve Member Invite Flow and Add Email Sending

**Requested by:** Daniel Bohlin
**Date:** 2026-03-23

## Goal
Improve the team member invitation experience: make name mandatory (not email), support email sending when configured, and always show a copy-link fallback.

## Changes

### 1. Make email optional, name mandatory
- Name should be the required field (to identify the invited member in the team list)
- Email should be optional — when provided, an invitation email should be sent

### 2. Email sending
- When email is provided and email sending is configured, send an invitation email with the invite link
- If email sending is not configured, show a clear message: "Email sending is not configured. Copy the invite link below and send it manually."
- The invite link should always be available for manual copying regardless of email configuration

### 3. Implement an email sender abstraction
- Add an `IEmailSender` (or similar) abstraction that consumers can configure
- Provide a default implementation (e.g. SMTP) or allow consumers to plug in their own
- Registration via `AddThargaPlatform()` options or a dedicated `AddThargaEmailSender()` call

## Acceptance Criteria
- [ ] Name is required, email is optional in invite dialog
- [ ] Invite link is always visible for manual copying
- [ ] When email is provided and IEmailSender is configured, an email is sent
- [ ] When IEmailSender is not configured, a clear message is shown
- [ ] IEmailSender abstraction is available for consumers to implement

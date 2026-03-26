# Feature: Copy Claims as JSON (Request #12)

## Goal
Add a copy button to UserProfileView that copies all claims and principal info to clipboard as pretty-printed JSON.

## Scope
- Add a button next to the "Claims" expandable card header
- On click, serialize identity name, authentication type, and all claims (type + value) to pretty JSON
- Copy to clipboard via JS interop
- From: Tharga.Platform (self-request) — Priority: Low

## Acceptance Criteria
- [ ] Copy button visible in the Claims section
- [ ] Clicking copies well-formatted JSON to clipboard
- [ ] JSON includes identity name, auth type, and all claims

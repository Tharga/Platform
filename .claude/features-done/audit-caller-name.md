# Feature: AuditLogView Should Show Caller Name, Not ID

## Goal
Show human-readable caller names in AuditLogView instead of raw user IDs or API key identifiers.

## Changes
- Added caller name cache that resolves user identities via IUserService on init
- Caller column shows resolved display name with raw ID as tooltip
- Top Callers chart groups by resolved name
- CSV export includes both Caller (display name) and CallerID (raw)
- Graceful fallback to raw ID when name resolution fails

## Completed: 2026-03-24

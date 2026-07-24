# Plan: Audit log error details on the "OK" column

## Steps

### Dependency update (up-front, mandatory)
- [x] 1. `SixLabors.ImageSharp` bump — **held at 3.1.12 (user decision 2026-07-24).** 4.0.0 enforces a
      build-time Six Labors license (build fails: "No Six Labors license found"); the bump was only a
      "Nice" item and usage is trivial, so not worth a paid/keyed license. Added a one-line rationale
      comment at the pin; recorded a project memory so future `dotnet outdated` sweeps don't re-flag it.

### Feature: OK-column error details
- [x] 4. Added `internal static BuildFailureCode` + `BuildFailureDetail` on `AuditLogView` (`.razor.cs`).
      Code = EventType for classified failures (ScopeDenial/AccessLevelDenial/AuthFailure/RateLimit),
      "Error" for a plain exception; null for success. Detail = code + Scope (checked + result) + Reason.
- [x] 5. OK column (`AuditLogView.razor`): success = green check; failure = red icon + code text with the
      detailed `title` hover tooltip. Column widened 50px → 160px.
- [x] 6. Added `AuditFailureDetailTests` (success, null, 4 classified codes, 2 exception codes, 2 detail shapes).
- [x] 7. Full suite green: `dotnet test -c Release` → 868 passed, 0 failed (Blazor 315 incl. 10 new).

### Finalize (on user confirmation only)
- [ ] 8. Re-run `dotnet outdated`; apply any new updates.
- [ ] 9. Update Tharga.Team.Blazor README + docs audit section.
- [ ] 10. Archive feature.md to Plan `done/`, `git rm -r plan`, close-out commit, push, open PR.

## Last session
Branch `feature/audit-error-details` created from master. ImageSharp bump abandoned (held at 3.1.12 —
4.0 build-time license gate; user decision). Audit OK-column error-details feature implemented + full
suite green (868). Not yet committed. Next: commit milestone, then finalize (docs + close-out) on user confirmation.

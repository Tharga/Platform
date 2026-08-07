# Plan: register the email sender from the granular path (#176)

## Steps

- [x] 1. NuGet check (mandatory, up front). Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held for its paid
      build-time licence. Standing exception, nothing to apply.

- [x] 2. `ThargaBlazorOptions` gains `Email`, `_emailSenderType` and `AddEmailService<T>()`, mirroring the
      facade's members so the two surfaces read the same.

- [x] 3. `RegisterEmail` in `AddThargaTeamBlazor` — custom sender > SMTP > nothing, called beside
      `RegisterIcons`. Copies `EmailOptions` whole via `OptionsForwarder` rather than property-by-property,
      then assigns `FromName` after, since it alone has a fallback.

- [x] 4. Facade forwards instead of registering. The old block in `ThargaTeamRegistration` is replaced by a
      comment pointing at the new home, matching what #157 left behind for icons. Forwarding is conditional
      (`if (options.Email != null)`) so a host configuring `o.Blazor.Email` is not clobbered.

- [x] 5. Tests — 9 new, 12 total across both paths.
      `GranularEmailRegistrationTests` (7): custom sender registered; SMTP registers the built-in; neither
      registers nothing; custom wins over SMTP; `FromName` falls back to `Title`; an explicit `FromName` is
      kept; and every `EmailOptions` property reaches the container, driven from the type's own shape so a
      new property is covered without anyone remembering.
      `EmailRegistrationTests` (+2): the facade's `FromName` fallback still resolves after moving layers, and
      email configured on `o.Blazor.Email` is honoured rather than overwritten.

- [x] 6. Build + full suite. **1819 tests pass**, 0 errors. Warnings back to the **11** baseline after fixing
      one I introduced — a `<see cref="Title"/>` that cannot resolve, since `Title` is inherited from
      `BlazorOptions` in another assembly. Changed to plain `<c>Title</c>`.

- [x] 7. Documentation. Email had **no documentation anywhere**, which is part of what #176 reported, so this
      is new content rather than an edit: a "Sending the invitation email" section in the implementation guide
      covering the three-way choice, both entry points, the `FromName` fallback, and the silent-degradation
      behaviour. `ITeamEmailSender`'s XML docs rewritten to state that invitations are the only mail the
      toolkit sends — Eplicta's explicit doc request.

- [ ] 8. Close-out: archive `feature.md` to the Plan directory `done/`, `git rm -r plan`, final commit, push,
      open the PR. **Only when the user confirms.**

## Notes / decisions

- **Folded onto `ThargaBlazorOptions`, not a standalone `AddThargaTeamEmail`.** Recorded in `feature.md`; the
  short version is that a separate extension leaves the two paths free to drift again.
- **The facade's field-by-field `EmailOptions` copy was the #177 defect in a second place.** Fixed here rather
  than filed, since the code was being moved anyway and `OptionsForwarder` already existed.
- No behaviour change for any existing consumer: every addition is additive and the facade path is covered by
  the tests it already had, plus two new ones for the parts that moved.

## Last session

Steps 1–7 complete. Nothing pushed, no PR. Next: close-out on confirmation.

Still open after this: plan 01 §3b (startup registration sweep) and §6b (`TeamAccessInterceptor`); GitHub #155
and #142; the `ITeamCache` forwarding-hazard decorator; and the release gate — nothing has published since
3.10.6, so three merged PRs are still not in consumers' hands.

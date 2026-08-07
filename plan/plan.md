# Plan: Eplicta 3.8.3 defect sweep

## Steps

- [x] 1. NuGet package check (mandatory, up front).
      `dotnet outdated` reports one update: `SixLabors.ImageSharp` 3.1.12 → 4.0.0 in `Tharga.Team.Images`.
      **Deliberately not applied** — 4.0+ requires a paid Six Labors build-time licence. Standing
      exception, not a deferral. Nothing else outdated, so no upgrade step to verify.

- [~] 2. **#175** — honour `TeamKey` at the audit access gate.
      `AuditLogView.QueryAsync` resolves the effective team as `query.TeamKey ?? PinnedFilter?.TeamKey`;
      add `?? TeamKey`. Fixing it in `QueryAsync` rather than at the probe covers every call site.
      Tests: parameter alone authorizes; pinned still wins; no team still reaches the oversight service.

- [ ] 3. **#177** — forward all of `IconOptions`.
      `RegisterIcons` copies only `MaxBytes` and `AllowedContentTypes`. Copy the whole instance so a
      property added later cannot be silently dropped — the same correction
      `ThargaBlazorOptionsForwarder` makes one layer up. Verify both entry points: the facade forwards
      `o.Icon = options.Icon` at `ThargaTeamRegistration.cs:87`, so both converge on this one site.
      Test: all four properties arrive, from both paths.

- [ ] 4. **#176** — expose the email sender from the granular path.
      Add `Email` (an `EmailOptions`) and `AddEmailService<T>()` to `ThargaBlazorOptions`; move the
      three-way registration (custom type > SMTP > nothing) into `AddThargaTeamBlazor`; have
      `AddThargaTeam` forward its own `options.Email` and `_emailSenderType` down, mirroring how icons are
      forwarded for #157. Keep `ThargaTeamOptions.Email` working for existing hosts.
      Tests: each of the three outcomes, from both the granular and facade paths.

- [ ] 5. Documentation.
      Implementation guide: the granular setup section does not mention that email needs separate wiring —
      fix that, and state that `SendInviteAsync` is the only mail the toolkit sends (Eplicta's doc point in
      #176). Check the icon docs still describe all four `IconOptions` correctly.

- [ ] 6. Build and full suite (`dotnet build -c Release`, `dotnet test -c Release`). No new warnings.

- [ ] 7. Close-out: archive `feature.md` to the Plan directory `done/`, `git rm -r plan`, final commit, push,
      open the PR. **Only when the user confirms the feature is done.**

## Notes / decisions

- **#176 API shape: fold onto `ThargaBlazorOptions`, not a standalone `AddThargaTeamEmail`.** This was
  offered to the user as an open decision and the recommendation was to follow the #157 precedent; taken
  on that basis. It keeps one configuration surface for both paths instead of two that can drift.
- **Order matters.** #175 first and committed on its own, so a tier-1 authorization fix is shippable even
  if #176's public surface needs another conversation.
- Branched from `master`, not from the cache branch — the three defects are independent of PR #198. Minor
  merge risk in `ThargaBlazorRegistration.cs`, which both touch in different methods.

## Last session

Branch created off `master`, working tree clean, plan agreed. Package check done — nothing to apply.
In progress: step 2 (#175).

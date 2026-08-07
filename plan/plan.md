# Plan: Eplicta 3.8.3 defect sweep

## Steps

- [x] 1. NuGet package check (mandatory, up front).
      `dotnet outdated` reports one update: `SixLabors.ImageSharp` 3.1.12 → 4.0.0 in `Tharga.Team.Images`.
      **Deliberately not applied** — 4.0+ requires a paid Six Labors build-time licence. Standing
      exception, not a deferral. Nothing else outdated, so no upgrade step to verify.

- [x] 2. **#175** — honour `TeamKey` at the audit access gate. **Done, committed `00681ac`.**
      Resolved in `QueryAsync` rather than at the probe, so every call site benefits. Precedence extracted
      to `AuditTeamScope.Resolve(query, pinned, parameter)` so it is asserted by tests rather than
      described in a comment — the same shape as `AuditFilterVisibility`. 8 tests: parameter alone
      authorizes, pin outranks parameter, query outranks both, no team stays system-wide, and empty is not
      a team (a host renders a blank parameter before its own state resolves).

- [x] 3. **#177** — forward all of `IconOptions`. **Done.**
      `RegisterIcons` now copies the whole instance via a new `OptionsForwarder.Copy`. Both entry points
      converge here, since the facade forwards `o.Icon = options.Icon` at `ThargaTeamRegistration.cs:87`.
      3 tests, and the third is the one that matters: it drives itself from `IconOptions`'s own properties,
      so a property added later is covered without anyone remembering, and a property whose type the test
      cannot set **fails loudly** rather than being skipped.
      *Not done:* refolding the existing `ThargaBlazorOptionsForwarder` onto the new shared helper. It has
      its own `NotForwarded` contract and a test bound to its API; consolidating is a tidy-up with no
      consumer benefit and is beyond this fix. Worth doing separately.

- [~] 4. **#176** — expose the email sender from the granular path.
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

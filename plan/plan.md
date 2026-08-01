# Plan: Enforce `team:read` on team reads

## Steps

- [x] **Update NuGet packages** — mandatory first step. `dotnet outdated` reports only
      `SixLabors.ImageSharp 3.1.12 -> 4.0.0`, deliberately held (4.0+ requires a paid Six Labors
      build-time license). Nothing applied; nothing else outdated.
- [x] **Establish the defect and its blast radius.** `TeamScopes.Read` registered and never enforced on a
      read; invisible for Viewer-and-above, real for `AccessLevel.Custom`.
- [x] **Find what a naive gate would break.** Three paths: two claims-bootstrap call sites and the invite
      flow. Documented in `feature.md`.
- [ ] **Confirm the approach with the user** — the internal-path design, and whether the invite read
      becomes invite-code-authorized in this feature or is split out.
- [ ] **Add the internal read path** and move `TeamMembershipClaimsBuilder` and
      `TeamClaimsAuthenticationStateProvider` onto it.
- [ ] **Add the invite-code-authorized team read** and move `TeamInviteView` onto it.
- [ ] **Gate the per-team reads** in `AuthorizationTeamServiceDecorator`:
      `GetTeamByKeyAsync`, `GetTeamAsync<TMember>`, `GetMembersAsync`, `GetTeamMemberAsync`, and the
      roster-carrying `GetTeamsAsync<TMember>`.
- [ ] **Leave non-generic `GetTeamsAsync()` ungated** — self-service, carries no members.
- [ ] **Tests**: a `Custom` caller refused each gated read; a Viewer member unaffected; both bootstrap
      paths still working; invite acceptance still working.
- [ ] **Verify the tests fail for the right reason** — remove a gate and confirm the failure names it.
- [ ] **Verify** — full suite plus an explicit sample compile (`-t:Compile`).
- [ ] **Bump `MAJOR_MINOR`** — behaviour-changing for `Custom` callers.
- [ ] **Docs** — both surfaces; the Service README's key-boundary table gains the read rule, and the
      `Custom` documentation should say the least-privilege promise is now actually kept.
- [ ] **Update spec 06** — this resolves the phase-3 question that was blocking the matrix.

## Notes

**The question that started this was better than the answer I gave.** I had framed phase 3 as "should
reading a team's own data be scope-gated at all?" — presenting as an open design decision something the
project had already decided and documented. The user's "the scope for the selected team should decide what
data could be read or modified, right?" is exactly what `TeamScopes.Read`'s own description promises. There
was no decision to make, only an unenforced rule.

**Why the bootstrap paths matter more than the gate.** Adding the gate is a few lines. Finding that
`TeamMembershipClaimsBuilder` reads a member while constructing the very claims that would authorize the
read is the whole difficulty — a gate added without that lands as "sign-in is broken" rather than as a
compile error.

**The invite read is a second finding.** `TeamInviteView` reads a team by naming its key, with nothing
checking the caller. Whatever happens with scopes, that path should authorize on the invite code.

## Last session

Investigation complete, plan written, awaiting confirmation on the approach before any code.

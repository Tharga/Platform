# Plan: PlutusWave defect batch

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **Run `dotnet outdated` across the whole solution.** Done 2026-08-01, before the branch was cut.

  **Result: one update available, deliberately NOT applied.**
  `SixLabors.ImageSharp` 3.1.12 → 4.0.0 in `Tharga.Team.Images`. ImageSharp 4.0+ requires a **paid
  Six Labors build-time licence**, so the hold is a licensing decision, not deferred maintenance.
  Everything else in the solution is current.

  This matters for step 6 — the icon squaring work stays on the 3.1.x `ResizeOptions` API.

- [x] **Verify build + full test suite on the untouched branch.** Baseline established before any
  feature code, so a later failure is attributable.

---

## 2. ~~`BlazorTeamPrincipalAccessor` throws outside the HTTP flow~~ — DROPPED, ALREADY SHIPPED

- [x] **Verified 2026-08-01: this was fixed before the branch existed.** Commit `f9e4ce6`,
      *"fix: resolve the caller through one helper that tolerates having no circuit"*, is contained in
      tag **3.8.1**. The current implementation returns
      `CircuitPrincipal.GetUserOrNullAsync(...)` when there is no `HttpContext`, and its XML remarks
      already document the MCP case by name.

**Why it was in this plan at all — worth recording so it does not happen twice.** The item came from
plan `06-audit-access-verification`, whose phase 1 still describes the defect as live. **The plan spec
lags master.** It was written before the fix and never revised. Nothing in the planning documents is
authoritative about what is *in* the product — only the code and the tags are.

**Same class of error found in the same check:** plan 06 phase 2 (REST audit endpoints) is also
already merged, and the `Tharga.Team.Mcp` API-key scheme contribution is merged and unreleased. All of
it now belongs to the release described in `plan/feature.md` → superseded-by note.

---

## 3. `UsersListView` — no self-delete — DONE 2026-08-01

- [x] `UserAdminGate.CanDeleteUser(rowUserKey, currentUserKey)` — pure, alongside `CanDeleteTeams`.
- [x] Delete item disabled on the caller's own row. `_currentUserKey` already existed (it drives the
      current-user row highlight), so nothing new had to be resolved.
- [x] Guard repeated in `DeleteUserAsync`. **Not redundant:** `ActionItems` lets a host inject an item
      and the handler dispatches on its value, so a host-supplied `"delete"` reaches the delete path
      without passing the markup gate.
- [x] 10 tests: another user allowed, own row refused, case-differing key refused, and five
      unknown-identity cases.

**Two decisions taken while building:**

- **Tooltip was not available, so the reason went in the label.** `RadzenSplitButtonItem` (Radzen
  10.4.7) has `Disabled` but no tooltip property — verified against the shipped XML docs, not assumed.
  A disabled control with no stated reason reads as a bug, so the label becomes
  `"Delete (this is you)"`. The row is already highlighted as the current user, which reinforces it.
- **The gate is `OrdinalIgnoreCase`, deliberately stricter than `MemberHighlight.IsCurrentMember`.**
  That one is case-sensitive and drives a highlight, where a false positive is cosmetic. This guards a
  destructive action, where a false negative deletes an account — so where they disagree, the guard is
  the stricter. Both the divergence and the reason are in the remarks and asserted by a test.
- **Fails closed on unknown identity.** A null key on either side refuses rather than allows. The view
  already requires `users:manage` so an authenticated caller always resolves; a null means identity
  could not be established, which is not a state in which to offer an irreversible action.

---

## 4. `TeamSelector` — respect `AllowTeamCreation` — DONE 2026-08-01

- [x] New `TeamSelectorGate.ShowCreateTeamLink(teamCount, allowTeamCreation)` — internal and pure,
      mirroring `MemberHighlight`. `_allowTeamCreation` read from options in `OnInitializedAsync`,
      exactly as `TeamComponent` does it.
- [x] **Both link variants covered.** The gate wraps the whole teamless block, so the
      `CreateTeamRequested` callback branch and the `RadzenLink` branch are gated by one condition —
      they cannot drift apart.
- [x] 8 tests, including one asserting the selector now agrees with `TeamComponent` for a teamless
      caller. The two contradicting each other is what made this a defect rather than a missing
      feature, so the agreement is the thing worth pinning.

**A structural trap avoided, worth recording.** The obvious edit — changing
`else if (_teams.Length == 0)` to `else if (ShowCreateTeamLink(...))` — is wrong. With no teams and
creation disabled it falls through to the `_teams.Length == 1` test, fails that too, and lands in the
final `else`, rendering the team **dropdown for a caller with zero teams**. The gate has to nest
*inside* the teamless branch, not replace it. A pure-function test would not have caught this; only
reading the branch chain does.

**Related, noted but not built here:** a caller holding `teams:read` with no membership gets no
affordance for the teams they may see, because the teamless branch assumes teamless means new user.
That is a third case, not a fix to this one — file it separately if wanted.

---

## 5. Entra directory fails safe when unconfigured — DONE 2026-08-01

- [x] `IUserDirectoryService.IsConfigured` as a **default interface member** (`=> true`), so hosts with
      a custom directory implementation need no change. Same on `IEntraTokenProvider`, for hosts with a
      custom token provider — asserted by a test using a provider that does not implement it.
- [x] `CredentialEntraTokenProvider.IsConfigured` and `CreateCredential` now read the same fields
      through one `HasCredentials` helper, so they cannot drift apart.
- [x] `EntraUserDirectoryService.IsConfigured` delegates to the token provider — nothing else about it
      can be half-configured, since the Graph address and scope both have working defaults.
- [x] Both call sites (`UsersListView`, `UsersView`) now ask
      `GetService<IUserDirectoryService>() is { IsConfigured: true }`. **No signature change to
      `UserAdminGate.ShowDirectoryFeatures`** — its second parameter already meant "is a usable
      directory present"; only the value feeding it was wrong. Doc updated to say so.
- [x] 15 tests: every partial credential combination, custom-credential-instead-of-secret, the
      service-delegates-to-provider pair, and the legacy-provider default.

**The throw stays, deliberately.** A host calling the service directly still gets the
`InvalidOperationException` naming the three settings — and a test pins that the two agree, so a
provider reporting configured can never then throw. The UI simply stops offering the button first.

**Open question for the user — should this also be loud?** Hiding is what the request asked for and is
right for the *operator*: a Verify button that always throws is the same defect as the per-team buttons
that threw when clicked. But it is silent for the *developer*, which is exactly the pattern PlutusWave
objected to in the triage. A startup throw would be loudest but could take a running host down on
upgrade, which is too aggressive for a minor. A one-time startup **warning** naming the missing
settings is the likely answer — **not built, needs a decision.**

---

## 6. Square uploaded icons by padding — DONE 2026-08-01

- [x] `ResizeMode.Pad`, `side = Math.Min(Math.Max(width, height), max)`, `PadColor = Color.Transparent`.
- [x] Early return fixed — the condition is now "already square **and** within bounds".
- [x] 9 tests: 1000×500 → 256×256, 100×50 → 100×100, 50×100 → 100×100, 300×300 → 256×256, square
      within bounds passes through, SVG/undecodable untouched, max=0 disabled, **padding is transparent**
      and **content is not cropped**.
- [x] Upload dialog wording updated in `TeamIconDialog` and `UserIconDialog` — they promised only
      downscaling, which is now an incomplete description of what an upload produces.

**Two things found while building that the spec did not mention:**

- **Loading as `Rgba32` is required, not incidental.** `Image.Load(data)` keeps the source pixel format,
  and a JPEG decodes to RGB with no alpha channel — padding that with `Color.Transparent` yields black
  bars. `Image.Load<Rgba32>(data)` forces an alpha channel so the padding is genuinely transparent.
  Pinned by `Padding_IsTransparent`, which asserts alpha 0 in the padded band and 255 in the content.
- **No upscaling is guaranteed by the formula, not by a flag.** `ResizeMode.Pad` *will* enlarge a source
  to fill its box. It never does here because the box side equals the source's own long side whenever
  that fits within `MaxDimension`, making the fit-inside scale exactly 1. Worth stating, because
  changing `side` to `max` would silently start upscaling every small icon.

## 6b. `MAJOR_MINOR` — NOT bumped, by the user's decision (2026-08-01)

Left at `3.8`, so this batch releases as **3.8.3**. The 2026-07-31 design note called for a minor bump
because squaring silently changes stored output for new uploads.

**The mitigation is the release note, and it is now doing work the version number will not.** The PR
description must lead with the behaviour change — squaring is on by default, already-stored icons are
not reprocessed, and a consumer relying on aspect-ratio-preserved output will see different images for
new uploads. This repo has been here before: 3.5.3 carried a breaking claim-provenance change under a
patch number, and the note in `Requests.md` still has to tell people *"do not infer 'safe patch
upgrade' from the number here."*

---

## 7. Documentation

- [ ] `README.md` — review and update where this changes documented behaviour.
- [ ] `docs/` — same review. **Both surfaces, not one:** step 5 changes what "configured" means for
      the directory, and step 6 changes what uploading an icon produces.
- [ ] Decide whether icon processing warrants its own `docs/articles/` page rather than an edit to an
      existing section.
- [ ] Land as a separate `docs:` commit before close-out.

---

## 8. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated` — new updates may have published since step 1. Apply and include them
      in this PR.
- [ ] Full suite green.
- [ ] Archive `plan/feature.md` to `$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/plutuswave-defect-batch.md`
- [ ] `git rm -r plan`
- [ ] Final commit: `fix: plutuswave-defect-batch complete`
- [ ] Push, open the PR against `master`. PR description is the release note — write it for package
      consumers, and **say that already-stored icons are not reprocessed**.

---

## Backlog / requests hygiene (after the PR merges, not before)

- [ ] Mark the four PlutusWave requests Done in `Requests.md` with date and summary — **only after
      user confirmation**, never unilaterally.
- [ ] Add the `## Follow-up` entry so PlutusWave knows to upgrade.
- [ ] Remove the completed entries from the backlog file.

---

## Last session

**2026-08-01 (setup).** Branch cut off `master`. Package check done — ImageSharp 4.0.0 available and
deliberately held for the paid-licence reason, everything else current. **Plan confirmed by the user.**

**Pre-flight runs before step 2, by the user's decision.** Two parts, both outside this branch:

1. **Triage sent.** The three PlutusWave reports (#12) are now a two-way document at
   `$DOC_ROOT/Tharga/PlutusWave/team-triage-2026-08-01.md`, status `AWAITING PLUTUSWAVE`. PlutusWave
   answers in the file; we record outcomes in the same file. Whatever survives becomes its own item —
   it does **not** get added to this branch late.
2. **Three decisions** put to the user: the owner-reassignment scope (gates the next feature), system
   API-key consent (gates plan 06), and the Slack event allowlist (gates plan 04). None of the three
   affects this branch — recorded here so the sequence is reconstructable, not because step 2 waits on
   them.

**Next:** step 2 — `BlazorTeamPrincipalAccessor`. Nothing in steps 2–6 is blocked.

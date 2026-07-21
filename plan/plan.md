# Plan: Highlight the current user in the team member list

Branch `feature/highlight-current-member` off `master`. See `feature.md`.

## Steps

- [x] **1. NuGet updates (up front)** — no-op. `dotnet outdated` clean (checked 2026-07-21). Re-check at close-out.

- [x] **2. Pure helper + tests** — done, `MemberHighlight.IsCurrentMember` (+8 tests).
- [x] **3. Wire the row tint** — done. `RowRender="@OnMemberRowRender"` on the member grid sets an inline
  row background (`var(--rz-primary-lighter, rgba(59,130,246,.12))` — theme token with a theme-neutral
  translucent fallback, since the project has no scoped CSS and no guaranteed token). Inline style beats
  Radzen's class-based stripe.
- [x] **4. "You" chip** — done. `RadzenBadge` "You" (Info, Flat) after the name in the read-only branch,
  gated on `MemberHighlight.IsCurrentMember`.
  Suite **669 green** (+8); sample boots and serves with the change. Visual (tint + chip on `/team`)
  is the user's manual test — needs Azure AD + browser.

  Original step text for 2–4:
  `Features/Team/MemberHighlight.cs` — `internal static bool IsCurrentMember(string memberKey, string userKey)`,
  false on null/empty either side, ordinal match. Tests cover match, non-match, and both-null (must not match).

- [ ] **3. Wire the row tint into `TeamComponent`**
  - Add `RowRender="@OnMemberRowRender"` to the member `RadzenDataGrid`.
  - `OnMemberRowRender(RowRenderEventArgs<TMember> args)` adds a CSS class to the row when
    `MemberHighlight.IsCurrentMember(args.Data.Key, _user?.Key)`.
  - Tint via a theme token (`var(--rz-primary-lighter)` or similar) so it holds in light and dark. Prefer a
    scoped-CSS class over an inline style so Radzen's row hover/stripe doesn't fight it; check whether the
    project already has a `.razor.css` and follow that, else inline style on the row attributes.

- [ ] **4. "You" chip in the name column**
  In the read-only branch of the Name column template, when the row is the current user, render a small
  `RadzenBadge` ("You", `Variant.Flat`, muted/info style) after the name.

- [ ] **5. Build + full suite** — `dotnet build -c Release`, `dotnet test -c Release`.

- [x] **6. Docs** — added a one-line note to the README "Team management" bullet (tints own row + "You"
  chip). No API, no config, so nothing else to document; the implementation guide needs no change.

- [ ] **7. Push, hand to user for testing in the sample** — do NOT open the PR yet. The sample already
  renders `TeamComponent`, so the current dev user's row should highlight with no sample change.

- [ ] **8. Close-out** — after user confirms: re-run `dotnet outdated`, archive `feature.md` to Plan
  directory `done/`, `git rm -r plan`, final commit `feat: highlight-current-member complete`, push, PR.

## Decisions

- **Row tint + "You" chip** (user, 2026-07-21) — colour for fast scanning, chip for accessibility/clarity.
- **Member list only**, no config, on by default (see feature.md non-goals).

## Fix (2026-07-21) — highlight was invisible: styled the row, not the cells

User saw no highlight at all. Real cause: Radzen `<td>` cells carry their own background and paint over a
`background` set on the `<tr>` via `RowRender` — so a row-level tint is never visible. Fixed by marking the
row with a `data-tharga-current-member` attribute in `RowRender` and styling the **cells** via a `<style>`
block (`tr[...] > td { background-color: rgba(37,99,235,.16) !important }` + an inset left accent on the
first cell). Translucent colour composites correctly on light and dark. This is why the earlier inline-style
attempts (even with a visible colour) showed nothing.

Separately found the sample had no theme switching at all (loaded `material-base.css` with no `<RadzenTheme>`
or theme service), so dark theme was untestable — fixed under the sample changes below.

## Change (2026-07-21) — dropped the inline marker; row highlight only

Two iterations on the marker: a "You" text chip needs localization, then a `person_pin` icon read as
another button next to the edit pencil. Removed the inline marker entirely — the row highlight
(background tint + left accent) is now the sole cue. No translatable string, no button-like glyph.
`MemberHighlight` and its tests are unchanged. **Watch:** the tint is now colour/position only, so if the
background reads too subtle as the single indicator, strengthen the opacity or lean on the accent bar.

## Fix (2026-07-21) — tint was invisible

User couldn't see the highlight on the sample. Root cause: the tint used `var(--rz-primary-lighter, …)`,
but Radzen's Material theme **defines** that token and it resolves to a near-white shade, so the rgba
fallback never ran and the row was tinted imperceptibly. Data path is correct — the sample creates the
owner's member with `Key = user.Key` and `_user` is the current user, so the match is true (the "You" chip
was rendering). Switched to a direct translucent blue (`rgba(59,130,246,.14)`) plus an `inset` box-shadow
left accent, so it's visible on light and dark and unmistakable even if cell backgrounds flatten the tint.
Radzen 10.4.7; `RowRender` API confirmed correct.

## Last session

**2026-07-21.** Branch created; `dotnet outdated` clean (step 1 no-op). Plan written, awaiting confirmation.

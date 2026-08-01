# Feature: PlutusWave defect batch

**Branch:** `feature/plutuswave-defect-batch` (off `master`)
**Started:** 2026-08-01
**Release:** minor bump — see "Version" below

## Goal

Clear four of PlutusWave's ten open requests plus one live defect, in a single release. Every item is
small; bundling them avoids five separate release cycles for changes no one of which earns its own.

## Scope

Five items, in implementation order. The order is deliberate: the live defect first, then the guards,
then the one change that forces the version bump.

| # | Item | Source | Why it is in this batch |
|---|---|---|---|
| 1 | `BlazorTeamPrincipalAccessor` throws outside the HTTP flow | Plan 06 §Ph1 | The only item that is **broken now** rather than missing — every MCP `resources/read` fails |
| 2 | `UsersListView` lets an administrator delete their own account | Request, PlutusWave 2026-07-31 | Self-deletion is the likeliest route to an ownerless team |
| 3 | `TeamSelector`'s "Create team" link ignores `AllowTeamCreation` | Request, PlutusWave 2026-07-31 | Offers an action the service layer refuses |
| 4 | Entra directory should report "not configured" instead of throwing late | Request, PlutusWave (secondary point of the dropped B2C entry) | A half-configured directory registers cleanly and fails at first use |
| 5 | Square uploaded icons by padding the short side | Request, PlutusWave 2026-07-28 | Design settled 2026-07-31; nothing left to decide |

### Deliberately not in scope

- **#12 (the three PlutusWave reports: icons / rename / member-name pen)** — triage runs *before* this
  branch and belongs to PlutusWave. Two of the three are expected to close with no toolkit change.
  Whatever survives becomes its own item, not a late addition here.
- **#8, #13, #14** — the PlutusWave medium batch. Related to each other, not to these.

## Acceptance criteria

1. **MCP reads work.** A `resources/read` against a `team://` resource succeeds for an authenticated
   caller with no `HttpContext` in the async flow. Covered by a test that constructs the accessor
   without an `HttpContext` and asserts it resolves rather than throws.
2. **No self-delete.** The Delete action is suppressed (or disabled with a tooltip) on the signed-in
   caller's own row in `UsersListView`. Gate logic is pure and unit-tested, following the
   `TeamActionGate` / `UserAdminGate` precedent — markup ordering is not testable in this project.
3. **Create-team link respects the option.** With `AllowTeamCreation = false`, `TeamSelector`'s
   teamless branch renders no create link, matching `TeamComponent`'s existing behaviour.
4. **Directory fails safe.** With `TenantId` / `ClientId` / `ClientSecret` incomplete, the directory
   service reports "not configured" and its surfaces stay hidden, as when unregistered — instead of
   throwing `InvalidOperationException` on the first Graph call.
5. **Icons are squared.** `ResizeMode.Pad` with `side = Math.Min(Math.Max(width, height), max)` and a
   transparent pad colour. The early-return condition becomes "already square **and** within bounds".
   1000×500 → 256×256; 100×50 → 100×100 (no upscaling); 300×300 → 256×256. SVG and anything
   ImageSharp cannot decode still pass through untouched.
6. Full test suite green. No new warnings above the CI threshold.

## Version

**Minor bump.** Criterion 5 silently changes stored output for every consumer's new uploads, which is
a behaviour change even though no signature moves. `MAJOR_MINOR` in `build.yml` is hand-maintained —
bump it in this PR or the release publishes under the wrong number.

Already-stored icons are **not** reprocessed. Release note must say so.

## Done condition

All five acceptance criteria met, full suite green, `README.md` and `docs/` reviewed and updated where
this changes documented behaviour (criteria 4 and 5 both do), `plan/` removed in the close-out commit,
PR open against `master`.

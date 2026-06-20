# Feature: async IThargaTextProvider (GetAsync)

## Goal
Make the text-provider contract async so consumers can resolve UI strings from an async
content/localization backend (Eplicta's Quilt4Net content) without sync-over-async. Replace
`string Get(TextKey)` with `Task<string> GetAsync(TextKey)`.

Follows #101 (Tharga/Platform shipped `IThargaTextProvider.Get` in v3.0.5).

## Design (confirmed with user)
- **Replace** `Get` → `Task<string> GetAsync(TextKey key)` (clean async-only contract).
- Breaking vs 3.0.5, but no consumer has adopted it yet → **minor bump 3.0 → 3.1** (build.yml `MAJOR_MINOR`).
- Components render text synchronously, so each string is resolved in `OnInitializedAsync` into a field
  initialized to the English `TextKey.Default` — no empty flicker before the async lookup returns.

## Scope
- `IThargaTextProvider.GetAsync`; `DefaultThargaTextProvider` returns `Task.FromResult(key.Default)`.
- `LoginDisplay` (4 strings) + `TeamSelector` (2 strings) resolve into default-seeded fields.
- `SampleMenuTextProvider.GetAsync`.
- Tests updated to async; docs bridge example updated.
- `build.yml` `MAJOR_MINOR` 3.0 → 3.1.

## Acceptance criteria
- [ ] `IThargaTextProvider.GetAsync(TextKey) : Task<string>` (Get removed).
- [ ] Default returns `key.Default`; consumer override still wins (option + DI).
- [ ] Components show English default immediately, then the resolved (possibly translated) value.
- [ ] Sample (Swedish) still works via GetAsync.
- [ ] Unit tests updated to async; full suite green.
- [ ] Docs updated; `MAJOR_MINOR` bumped to 3.1.
- [ ] Build + full test suite green on net9.0/net10.0.

## Done condition
PR `feature/text-provider-async` → `master`; user confirms. Release will be **3.1.0**.

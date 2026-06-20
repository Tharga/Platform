# Feature: Standard text-provider contract + localizable team menu

## Goal
Add a single **standard injectable text contract** to Tharga.Team so component UI strings are
localizable through one override point, and wire the **menu** strings (`LoginDisplay`, `TeamSelector`)
to it. Default behavior is unchanged — English strings exactly as today when no provider is registered.

GitHub issue: Tharga/Platform#101. Consumer: Eplicta FortDocs (multilingual UI; routes all other text
through Quilt4Net content/localization and wants the Tharga menu strings to flow through the same system).

## Design (confirmed with user)
- `IThargaTextProvider { string Get(TextKey key); }` — the one contract every Tharga.Team component uses.
- `readonly record struct TextKey(string Key, string Default)` — bundles the lookup key with its English
  default so a call site can never drift from its fallback.
- `DefaultThargaTextProvider` (internal sealed) → returns `key.Default`. Registered once via
  `TryAddSingleton`, so a consumer override (e.g. bridging to Quilt4Net content) always wins.
- Per-component key catalogs (static classes of `TextKey`) are all a new component adds to become
  localizable — no new interface, no new registration. `TeamMenuText` is the first catalog.

## Scope
- Contract + default impl + `TeamMenuText` catalog.
- Register the default in `AddThargaTeamBlazor` (unconditional — `LoginDisplay` needs it even with no team service).
- Wire `LoginDisplay` (User / Team / Logout / Login) and `TeamSelector` (Create Team / Loading…).
- Tests + docs.

## Out of scope (foundation enables later)
- Wiring other components (`TeamComponent`, `ApiKeyView`, `UsersView`, `AuditLogView`, …). Each just
  adds a `TextKey` catalog and swaps literals when someone asks.

## Acceptance criteria
- [ ] `IThargaTextProvider`, `TextKey`, `DefaultThargaTextProvider`, `TeamMenuText` exist in `Tharga.Team.Blazor.Framework`.
- [ ] Default registered automatically; a consumer-registered `IThargaTextProvider` overrides it (either registration order).
- [ ] `LoginDisplay` + `TeamSelector` render via the provider; with no override the visible text is unchanged English.
- [ ] Navigation still keys off icons (localizing Text does not break menu actions).
- [ ] Unit tests: default returns `key.Default`; registration default + override; `TeamMenuText` key/default values.
- [ ] Docs updated (implementation-guide: localization section + example bridge).
- [ ] Build + full test suite green on net9.0/net10.0.

## Done condition
PR `feature/team-text-provider` → `master`; user confirms after testing.

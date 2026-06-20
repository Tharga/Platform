# Plan: Standard text-provider contract + localizable team menu (Tharga/Platform#101)

- [x] 1. Contract: `IThargaTextProvider`, `TextKey`, `DefaultThargaTextProvider` (Framework, flat per convention)
- [x] 2. `TeamMenuText` key catalog (User/Team/Logout/Login/CreateTeam/Loading)
- [x] 3. Register default in `AddThargaTeamBlazor` (TryAddSingleton, unconditional, after AddThargaBlazor)
- [x] 4. Wire `LoginDisplay` (4 strings) + `TeamSelector` (2 strings) via injected `TextProvider`
- [x] 5. Tests: default impl, registration default+override (both orders), key catalog values (10 tests)
- [x] 6. Docs: implementation-guide "Localizing menu strings" section + consumer bridge example
- [x] 7. Build + full test run (461/461 green); commit (feat + docs)
- [x] 8. Sample: SampleMenuTextProvider (Swedish menu demo, dict + English fallback)
- [x] 9. Options-based registration: `o.Blazor.AddTextProvider<T>()` (mirrors AddClaimsEnricher); sample +
       docs use it; +1 test. Full suite 462/462.

## Notes
- NuGet checked during #100 finalization — Tharga.Team.Blazor deps current; nothing to bundle.
- `_Imports.razor` already imports `Tharga.Team.Blazor.Framework` + `Tharga.Team` → no per-component @using needed.
- Contract shape confirmed: keyed + `TextKey` (key + English default bundled).

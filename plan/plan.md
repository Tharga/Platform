# Plan: async IThargaTextProvider (GetAsync)

- [x] 0. NuGet up front — `dotnet outdated`: "No outdated dependencies". No-op.
- [x] 1. `IThargaTextProvider.Get` → `Task<string> GetAsync`; `DefaultThargaTextProvider` Task.FromResult
- [x] 2. `LoginDisplay` + `TeamSelector` resolve text in OnInitializedAsync into default-seeded fields
- [x] 3. `SampleMenuTextProvider.GetAsync`
- [x] 4. Tests async; docs bridge example async
- [x] 5. `build.yml` MAJOR_MINOR 3.0 → 3.1 (breaking change → minor bump)
- [~] 6. Build + full test run; commit; present for user testing

## Notes
- Replace (not add) per user; breaking vs 3.0.5 but no adopters yet.
- Component fallback: fields default to `TextKey.Default` so first render shows English before async resolves.
- Next release becomes 3.1.0.

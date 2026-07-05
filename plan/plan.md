# Plan: OpenAPI transformer hook on `AddThargaControllers` (#116)

Branch: `feature/openapi-transformer-hook` (from `master`, GitHub Actions strategy → PR to `master`)

## Steps

- [x] 1. **[Mandatory, first] Package bump.** `Tharga.MongoDB` 2.11.3 → 2.13.0 in
  `Tharga.Team.MongoDB` + `Tharga.Team.Service`. Done — build Release green, full suite green
  (7 + 280 + 51 + 164 = 502 tests, 0 failures).
- [~] 2. **Extend `ThargaControllerOptions`.** Add net10-guarded composable passthrough
  `ConfigureOpenApi(Action<OpenApiOptions>)` (accumulates into an internal delegate).
- [ ] 3. **Wire into `AddThargaControllers`.** Invoke the accumulated consumer delegate inside the
  existing net10 `AddOpenApi(o => …)`, after the API-key security scheme is added.
- [ ] 4. **Tests** (`Tharga.Team.Service.Tests`, net10) — new `AddThargaControllersOpenApiTests.cs`:
  document transformer invoked + mutates doc; operation-transformer path; no-op when unconfigured;
  API-key scheme coexists with a consumer transformer.
- [ ] 5. **Docs.** Update `Tharga.Team.Service/README.md` and `docs/articles/implementation-guide.md`
  with the hook usage (the `ScopeFilteringDocumentTransformer` example from the issue).
- [ ] 6. **Close-out** (only when user confirms done): re-check `dotnet outdated`; archive
  `plan/feature.md` → Plan `done/`; `git rm -r plan`; final `feat:` commit; push; open PR.

## Notes / decisions
- API shape: passthrough `ConfigureOpenApi(Action<OpenApiOptions>)` — confirmed with user 2026-07-05.
- net9 out of scope (Swashbuckle-only path unchanged; OpenApi v2 transformer types are net10).

## Last session
(current) — Branch created, plan files written. Next: step 1 (package bump + verify green).

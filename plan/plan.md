# Plan: OpenAPI transformer hook on `AddThargaControllers` (#116)

Branch: `feature/openapi-transformer-hook` (from `master`, GitHub Actions strategy → PR to `master`)

## Steps

- [x] 1. **[Mandatory, first] Package bump.** `Tharga.MongoDB` 2.11.3 → 2.13.0 in
  `Tharga.Team.MongoDB` + `Tharga.Team.Service`. Done — build Release green, full suite green
  (7 + 280 + 51 + 164 = 502 tests, 0 failures).
- [x] 2. **Extend `ThargaControllerOptions`.** Done — net10-guarded `ConfigureOpenApi(Action<OpenApiOptions>)`
  accumulates into internal `OpenApiConfigure` delegate; null-guard; returns `this` for chaining.
- [x] 3. **Wire into `AddThargaControllers`.** Done — `options.OpenApiConfigure?.Invoke(o)` inside the
  net10 `AddOpenApi(o => …)`, after the API-key security scheme.
- [x] 4. **Tests** — Done — new `AddThargaControllersOpenApiTests.cs`, 6 tests, all green:
  accumulation order, chaining, null-guard, null-when-unset, hook invoked against the SAME managed
  `OpenApiOptions` instance (`Assert.Same`), no-op path still materializes options. (Full document
  generation isn't unit-testable — `IDocumentProvider` is internal — verified via the sample app instead.)
- [x] 5. **Docs.** Done — new "Customizing the OpenAPI document" section in
  `Tharga.Team.Service/README.md`; new ".NET 10+" subsection under Step 3 in
  `docs/articles/implementation-guide.md`. Both show the `ScopeFilteringDocumentTransformer` example
  and explain why the hook is preferred over a direct `AddOpenApi("v1", …)` call.
- [ ] 6. **Close-out** (only when user confirms done): re-check `dotnet outdated`; archive
  `plan/feature.md` → Plan `done/`; `git rm -r plan`; final `feat:` commit; push; open PR.

## End-to-end verification (2026-07-05)
Threw up a minimal net10 web host in scratchpad referencing the real `Tharga.Team.Service`, registered a
marker document transformer via `o.ConfigureOpenApi(...)`, served `/openapi/v1.json`. Rendered document
carried BOTH the consumer mutation (`"title": "MARKER-CONSUMER-HOOK-9271"`) AND Tharga's own API-key
scheme (`securitySchemes.ApiKey` + `security: [{ApiKey: []}]`) — confirming the hook composes into the
managed document. Full suite: 508 tests green (was 502; +6 new).

## Notes / decisions
- API shape: passthrough `ConfigureOpenApi(Action<OpenApiOptions>)` — confirmed with user 2026-07-05.
- net9 out of scope (Swashbuckle-only path unchanged; OpenApi v2 transformer types are net10).

## Last session
Steps 1–5 complete. Package bumped (MongoDB 2.13.0), `ConfigureOpenApi` hook implemented + wired,
6 tests added, README + implementation-guide updated, end-to-end verified via a scratchpad host.
508 tests green. Awaiting user test/confirmation before close-out (step 6: re-check outdated,
archive feature.md, `git rm -r plan`, final `feat:` commit, PR).

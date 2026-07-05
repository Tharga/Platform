# Feature: OpenAPI transformer hook on `AddThargaControllers` (GitHub #116)

## Goal
Let a consumer register its own `IOpenApiDocumentTransformer` / `IOpenApiOperationTransformer`
(e.g. FortDocs' `ScopeFilteringDocumentTransformer`) through `AddThargaControllers`, wired into the
**same** OpenAPI document Tharga already manages — so consumers no longer call
`AddOpenApi("v1", …)` directly. This removes:
- the composition ambiguity (does the consumer's `AddOpenApi` compose with or override Tharga's?),
- the `<InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>`
  workaround the consumer csproj needs in .NET 10.

## Scope
- **net10 only.** The native `IOpenApiDocumentTransformer` path (and the interceptor problem) are
  .NET 10. The existing `AddOpenApi(...)` block in `ControllersRegistration` is already
  `#if NET10_0_OR_GREATER`. net9 keeps its current Swashbuckle-only path unchanged — the
  `Microsoft.OpenApi` v2 types used by the transformer aren't available there.

## API shape (confirmed with user)
Passthrough to `OpenApiOptions` — the ASP.NET-idiomatic, complete surface:
```csharp
builder.Services.AddThargaControllers(o =>
{
    o.ConfigureOpenApi(api => api.AddDocumentTransformer<ScopeFilteringDocumentTransformer>());
});
```
`ConfigureOpenApi(Action<OpenApiOptions>)` accumulates (multiple calls compose), guarded
`#if NET10_0_OR_GREATER`. Tharga invokes the accumulated delegate inside its own single
`AddOpenApi(o => …)` call, after registering the API-key security scheme.

## Acceptance criteria
- [ ] `ThargaControllerOptions.ConfigureOpenApi(Action<OpenApiOptions>)` exists (net10), composable.
- [ ] Consumer-registered document/operation transformers run against Tharga's managed document.
- [ ] Tharga's own API-key security scheme is still present when a consumer hook is also registered.
- [ ] No consumer `AddOpenApi("v1", …)` call and no interceptor-namespace workaround required.
- [ ] Tests cover: document transformer invoked + mutates doc; operation transformer path; no-op when
      unconfigured; API-key scheme coexists with a consumer transformer.
- [ ] README + implementation-guide document the hook with the `ScopeFilteringDocumentTransformer` example.
- [ ] `dotnet build -c Release` + `dotnet test -c Release` green.

## Done condition
All acceptance criteria met, docs updated, PR opened to `master` with a release-note-quality description.

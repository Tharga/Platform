# Feature: NuGet updates and MCP cleanup

## Goal
Bring every PackageReference across the solution to the latest stable, then drop the
obsolete `MapMcpPlatform` / `AddMcpPlatform` wrappers from `Tharga.Platform.Mcp`
(follow-up #55 in `Requests.md`).

## Originating branch
`master`

## Scope

### In scope
- Bump every `<PackageReference>` in every `.csproj` to latest stable, including:
  - **Tharga.*** — Tharga.Toolkit, Tharga.MongoDB, Tharga.Blazor, Tharga.Mcp
  - **Microsoft.*** — Extensions.DependencyInjection(.Abstractions), AspNetCore.Components.Authorization, AspNetCore.OpenApi, AspNetCore.DataProtection, Identity.Web, NET.Test.Sdk
  - **Other** — Swashbuckle.AspNetCore, System.Linq.Async, coverlet.collector, NSubstitute, Moq, xunit.v3, xunit.runner.visualstudio
- For framework-conditional refs (net9 vs net10), keep the conditional structure;
  bump each to the latest within that framework family.
- Adopt any minor source changes required by the new Tharga.Mcp once bumped
  (only if breaking; bump-only otherwise).
- Remove `MapMcpPlatform` (obsolete wrapper) and `AddMcpPlatform` (obsolete alias)
  from `Tharga.Platform.Mcp/McpPlatformBuilderExtensions.cs`.
- Verify the sample app still runs and that all unit tests pass.

### Out of scope
- Target framework changes (stay on net9.0;net10.0 for libraries, net10.0 for tests/sample).
- Functional changes beyond what's forced by package upgrades.
- Consumer-side follow-ups (PlutusWave, Quilt4Net.Server, etc. — those are listed
  in `Requests.md` "Follow-up" but live in their own repos).

## Acceptance criteria
- [ ] All `<PackageReference Version="...">` declarations point at the latest stable
      version available on nuget.org at time of merge.
- [ ] `dotnet build -c Release` succeeds with no new warnings beyond the existing
      `NoWarn` set.
- [ ] `dotnet test -c Release` passes for every test project in the solution.
- [ ] `MapMcpPlatform` and `AddMcpPlatform` are deleted from
      `Tharga.Platform.Mcp/McpPlatformBuilderExtensions.cs`. Consumers must use
      `AddPlatform` (registration) and `app.UseThargaMcp()` (mapping).
- [ ] Sample app launches and the `/mcp` endpoint responds (smoke test).
- [ ] Follow-up #55 in `$DOC_ROOT/Tharga/Requests.md` removed.
- [ ] README and any `Tharga.Platform.Mcp` docs no longer mention `MapMcpPlatform`.

## Compatibility notes
- Removing `MapMcpPlatform` / `AddMcpPlatform` is a **breaking** API change for any
  consumer still on the obsolete names. Per `CLAUDE.md`: "Suggest bumping of minor
  version when compatibility is broken." Bump minor on the next release.

## Done condition
All acceptance criteria met, plan archived to
`$DOC_ROOT/Tharga/plans/Toolkit/Platform/done/nuget-updates-and-mcp-cleanup.md`,
and the user has confirmed the feature is complete.

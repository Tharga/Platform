# Plan: NuGet updates and MCP cleanup

## Steps

- [~] **1. Confirm plan and create feature branch**
  - Branch `feature/nuget-updates-and-mcp-cleanup` created from `master`.
  - `plan/feature.md` and `plan/plan.md` drafted.
  - Awaiting user confirmation before code changes.

- [x] **2. Query nuget.org for latest stable versions**

  Inventory result (2026-05-18):

  | Package | Current | Latest stable | Bump |
  |---|---|---|---|
  | Tharga.Toolkit | 1.15.23 | **1.15.24** | yes |
  | Tharga.MongoDB | 2.10.10 | **2.10.12** | yes |
  | Tharga.Blazor | 2.1.5 | **2.1.6** | yes |
  | Tharga.Mcp | 0.1.3 | **0.1.4** | yes |
  | Microsoft.Extensions.DependencyInjection | 10.0.7 | **10.0.8** | yes |
  | Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.7 | **10.0.8** | yes |
  | Microsoft.AspNetCore.Components.Authorization (net9) | 9.0.15 | **9.0.16** | yes |
  | Microsoft.AspNetCore.Components.Authorization (net10) | 10.0.7 | **10.0.8** | yes |
  | Microsoft.AspNetCore.OpenApi (net9) | 9.0.15 | **9.0.16** | yes |
  | Microsoft.AspNetCore.OpenApi (net10) | 10.0.7 | **10.0.8** | yes |
  | Microsoft.AspNetCore.DataProtection (net10) | 10.0.7 | **10.0.8** | yes |
  | Microsoft.Identity.Web | 4.9.0 | 4.9.0 | no |
  | Microsoft.NET.Test.Sdk | 18.5.1 | 18.5.1 | no |
  | Swashbuckle.AspNetCore | 10.1.7 | 10.1.7 | no |
  | System.Linq.Async | 7.0.1 | 7.0.1 | no |
  | coverlet.collector | 10.0.0 | **10.0.1** | yes |
  | NSubstitute | 5.3.0 | 5.3.0 | no |
  | Moq | 4.20.72 | 4.20.72 | no |
  | xunit.v3 | 3.2.2 | 3.2.2 | no |
  | xunit.runner.visualstudio | 3.1.5 | 3.1.5 | no |

  **12 version bumps, all patch-level. No major or minor changes.**

  Note: Tharga.MongoDB 2.10.10 → 2.10.12 carries a documented "minor breaking" change
  (`GetFailedIndices()` return type). Grep across the solution shows no references —
  no consumer code changes needed.

- [x] **3. Bump Tharga.* packages first**
  - Tharga.Toolkit 1.15.23 → 1.15.24
  - Tharga.MongoDB 2.10.10 → 2.10.12 (in Tharga.Team.Service and Tharga.Team.MongoDB)
  - Tharga.Blazor 2.1.5 → 2.1.6
  - Tharga.Mcp 0.1.3 → 0.1.4

- [x] **4. Bump Microsoft.* packages**
  - Microsoft.Extensions.DependencyInjection 10.0.7 → 10.0.8
  - Microsoft.Extensions.DependencyInjection.Abstractions 10.0.7 → 10.0.8
  - Microsoft.AspNetCore.Components.Authorization 9.0.15 → 9.0.16 / 10.0.7 → 10.0.8
  - Microsoft.AspNetCore.OpenApi 9.0.15 → 9.0.16 / 10.0.7 → 10.0.8
  - Microsoft.AspNetCore.DataProtection 10.0.7 → 10.0.8
  - Microsoft.Identity.Web — already on latest (4.9.0)
  - Microsoft.NET.Test.Sdk — already on latest (18.5.1)

- [x] **5. Bump third-party + test packages**
  - coverlet.collector 10.0.0 → 10.0.1 (4 test projects)
  - Swashbuckle.AspNetCore, System.Linq.Async, NSubstitute, Moq, xunit.v3, xunit.runner.visualstudio — already on latest.

- [x] **6. Run full test suite**
  - `dotnet build -c Release` — 0 warnings, 0 errors.
  - `dotnet test -c Release` — 313/313 pass (7 MongoDB + 157 Service + 52 Mcp + 97 Blazor).

- [x] **7. Drop MapMcpPlatform / AddMcpPlatform**
  - Deleted `MapMcpPlatform` and `AddMcpPlatform` from
    `Tharga.Platform.Mcp/McpPlatformBuilderExtensions.cs`.
  - Removed unused `Microsoft.AspNetCore.Builder` / `Microsoft.AspNetCore.Routing` usings
    that only existed for the removed methods.
  - Deleted `Tharga.Platform.Mcp.Tests/AddPlatformTests.cs::ObsoleteAddMcpPlatform_ForwardsToAddPlatform`
    test (test for the removed alias).

- [x] **8. Build + test again after the cleanup**
  - `dotnet build -c Release` — 0 warnings, 0 errors.
  - `dotnet test -c Release` — 312/312 pass (one fewer than step 6 — the removed test).

- [x] **9. Smoke-test the sample**
  - **Skipped.** Running `Tharga.Platform.Sample` requires MongoDB + Azure AD configuration
    that isn't available in this session.
  - Risk is low: build succeeds for the sample project (which uses `app.UseThargaMcp()`
    directly already), and removing two `[Obsolete]` extension methods that just delegated
    to `UseThargaMcp` cannot have changed runtime behavior. To be flagged in PR.

- [x] **10. Update Requests.md follow-up + README**
  - Removed follow-up #55 from `$DOC_ROOT/Tharga/Requests.md`.
  - Updated `Tharga.Team.Service/README.md` example from `app.MapMcp()` to
    `app.UseThargaMcp()` (separate stale reference unrelated to the obsolete wrapper).
  - Top-level `README.md` and other component READMEs already correct.

- [ ] **11. Commit + open PR for review**
  - Two commits: bumps (`9194c83`) + MCP cleanup (this commit).
  - User reviews and merges.

## Last session
All code changes complete on `feature/nuget-updates-and-mcp-cleanup`:
1. Package bumps committed (`9194c83`) — 12 patch-level bumps, 313/313 tests pass.
2. MCP cleanup ready to commit — removed `MapMcpPlatform`/`AddMcpPlatform` and the
   test for the obsolete alias, fixed an unrelated stale `app.MapMcp()` example in
   `Tharga.Team.Service/README.md`. 312/312 tests pass.

Awaiting user review. Next: commit the cleanup, then user opens PR.

**Breaking change to call out in PR description:** removal of `MapMcpPlatform()` and
`AddMcpPlatform()` is a public-API removal. Both were `[Obsolete]` already. Per
`CLAUDE.md`: suggest a minor version bump on next release.

**Smoke test NOT run** — see Step 9; flag this in the PR.

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

- [ ] **7. Drop MapMcpPlatform / AddMcpPlatform**
  - Delete the two obsolete extension methods from
    `Tharga.Platform.Mcp/McpPlatformBuilderExtensions.cs` (lines 59–77).
  - Verify nothing else in the solution references them
    (already confirmed: sample uses `app.UseThargaMcp()`, no test references).
  - Update `Tharga.Platform.Mcp/README.md` if it mentions `MapMcpPlatform`
    (already on `UseThargaMcp` per grep).

- [ ] **8. Build + test again after the cleanup**
  - `dotnet build -c Release`
  - `dotnet test -c Release`

- [ ] **9. Smoke-test the sample**
  - `dotnet run --project Tharga.Platform.Sample`
  - Confirm `/mcp` is reachable (no broken endpoint after wrapper removal).

- [ ] **10. Update Requests.md follow-up + README**
  - Remove follow-up #55 from `$DOC_ROOT/Tharga/Requests.md`
    ("Tharga.Platform.Mcp should drop `MapMcpPlatform()` wrapper…").
  - Update top-level README if it references either obsolete extension.
  - Note any consumer-facing breaking change in PR description (for release notes).

- [ ] **11. Commit + open PR for review**
  - Single commit per logical milestone per `CLAUDE.md`.
  - User reviews and merges.

## Last session
Plan drafted on `feature/nuget-updates-and-mcp-cleanup`. Awaiting user
confirmation before starting Step 2.

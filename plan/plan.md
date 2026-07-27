# Plan: Rename Platform to Team

Branch: `feature/rename-platform-to-team` (from `master`)

## Steps

- [x] **1. NuGet package check (up front)** — `dotnet outdated` across the solution. Only
      `SixLabors.ImageSharp 3.1.12 -> 4.0.0`, held deliberately (4.0+ needs a paid Six Labors build-time
      licence). Nothing to apply.

- [x] **2. Baseline** — `dotnet test -c Release` **989 passing** across 6 projects (Images 4, Entra 17,
      MongoDB 39, Service 494, Mcp 51, Blazor 384). Build clean.

- [x] **3. Version line folded in** — cherry-picked `chore: move the version line to 3.6` from
      `chore/version-line-3-6` so 3.6.0 carries real content instead of being an empty marker release.
      **PR #154 must be closed as superseded** when this PR opens (its branch can then be deleted).

- [x] **4. Rename the MCP package** — done. 989 tests still green; the test assembly is now
      `Tharga.Team.Mcp.Tests.dll`. All moves used `git mv`, so git recorded them as renames (84-92% similarity)
      and history follows. CI `build.yml` pack path updated in the same commit — a miss there would have
      silently stopped shipping the package with no build or test failure.

  **`git grep` gave a false negative and nearly hid four references.** `git grep "a\|b"` does **not** treat
  `\|` as alternation — it matched nothing and read as "all clear". Re-running with `git grep -E` found
  `README.md`, the sample's `using`, its `AddPlatform()` call and its `ProjectReference`. **Use `git grep -E`
  for every sweep in step 9**, and never trust a clean alternation grep without first proving the pattern
  matches something known to exist.
  - Directory + csproj: `Tharga.Platform.Mcp` → `Tharga.Team.Mcp`, and `Tharga.Platform.Mcp.Tests` →
    `Tharga.Team.Mcp.Tests`. Use `git mv` so history follows.
  - `PackageId` → `Tharga.Team.Mcp`; check `Description`/`PackageTags` for the old product name.
  - Namespace `Tharga.Platform.Mcp` → `Tharga.Team.Mcp`.
  - Types: `PlatformMcpContext`, `PlatformTeamResourceProvider`, `PlatformSystemResourceProvider`,
    `PlatformUserResourceProvider`, `McpPlatformOptions`, `McpPlatformBuilderExtensions` — drop the
    `Platform` prefix/infix. Clean renames, no obsolete forwarding: the package ID is new, so a consumer
    arriving here is already migrating deliberately.
  - `IThargaMcpBuilder.AddPlatform()` → `AddTeam()`.
  - **`platform://` → `team://`** in every `…Uri` const, in `README.md` for the package, and in the tests.
    Grep for the literal afterwards — a missed URI fails at runtime, not at compile time.

- [ ] **5. Public API rename in `Tharga.Team.Blazor`** — additive, must keep compiling.
  - `ThargaPlatformRegistration` → `ThargaTeamRegistration`, `AddThargaPlatform` → `AddThargaTeam`,
    `ThargaPlatformOptions` → `ThargaTeamOptions`.
  - **Decide the forwarding shape during implementation.** The obvious version does not work:
    `AddThargaPlatform(Action<ThargaPlatformOptions>)` cannot simply delegate to
    `AddThargaTeam(Action<ThargaTeamOptions>)` if the options type also changed, because the callback is
    handed an instance the new method constructed. Two workable shapes — pick one and record why:
    (a) keep a single `ThargaTeamOptions` type and have the obsolete method take `Action<ThargaTeamOptions>`
    (loses the old *type* name, keeps every lambda call site compiling), or
    (b) `[Obsolete] class ThargaPlatformOptions : ThargaTeamOptions` plus an overload that builds that
    subtype and passes the instance through (keeps both names, needs an options-instance entry point).
  - `ThargaPlatformRegistration.IconHttpClientName` is referenced from `TeamIconDialog` and `UserIconDialog`
    — update both.
  - Verify the obsolete path still *behaves* identically, not merely compiles.

- [ ] **6. Solution and sample** — `Tharga.Platform.sln` → `Tharga.Team.sln`,
      `Tharga.Platform.Sample` → `Tharga.Team.Sample` (assembly name, root namespace, project references).
      Also `Tharga.Platform.sln.startup.json`. Point the sample at `AddThargaTeam` so the documented entry
      point is the one the sample demonstrates.

- [ ] **7. CI workflow** — `.github/workflows/build.yml` packs each project by explicit path; the renamed
      MCP project must be updated there or it silently stops shipping. Check for any other path reference.

- [ ] **8. Docs** — `docfx.json` `_appName`/`_appTitle`; `docs/CNAME` → `team.tharga.net`; sweep
      `docs/articles/**` and `README.md`. Keep "Platform" where it means the *package family*, not the
      product. Document the `team://` URIs and the `AddThargaTeam` entry point.

- [ ] **9. Full verification** — `dotnet build -c Release` + `dotnet test -c Release`, 989+ green. Then grep
      the whole tree for `Platform` and account for **every** remaining hit as either legitimate (package
      family, historical changelog, external URL) or missed.

- [ ] **10. Push** for the user to test from origin. Do **not** open the PR yet; do **not** close the feature.

## Hand-offs — actions only the user can perform

These are part of the feature and must not be quietly dropped. Sequence matters: the package must be
published before it can be deprecated against, and DNS must resolve before the old CNAME is removed.

- [ ] **A. Merge + publish 3.6.0**, so `Tharga.Team.Mcp` exists on nuget.org.
- [ ] **B. Deprecate `Tharga.Platform.Mcp`** on nuget.org with `Tharga.Team.Mcp` as its alternate. Deprecate,
      do **not** unlist — unlisting would break consumers who have not migrated.
- [ ] **C. Rename the GitHub repo** `Tharga/Platform` → `Tharga/Team`. GitHub redirects the old URL; I will
      update the local remote afterwards.
- [ ] **D. Add the `team.tharga.net` DNS record**, verify it resolves, and only then retire
      `platform.tharga.net`.
- [ ] **E. Optionally rename the local working directory** `C:\dev\tharga\Toolkit\Platform` — breaks absolute
      paths in `settings.local.json`, so it is deliberately not automated.

## Close-out (only when the user says it is done)

- [ ] Re-run `dotnet outdated`; apply and include in this PR.
- [ ] Archive `plan/feature.md` to `$DOC_ROOT/…/done/rename-platform-to-team.md`
- [ ] `git rm -r plan`
- [ ] Final commit `feat: rename-platform-to-team complete`, push, open PR
- [ ] Close PR #154 as superseded; delete `chore/version-line-3-6`
- [ ] Move `planned/03-rename-platform-to-team.md` out of `planned/`

## Notes

- **The MCP URI rename is a deliberate break**, chosen by the user over the spec's non-breaking assumption.
  It must be the most prominent item in the PR description, and it needs a `Requests.md` follow-up telling
  Quilt4Net.Server and PlutusWave — both track `Tharga.Platform.Mcp` upgrades there.
- Still outstanding and unrelated: delete the stale `planned/02-authorization-defects.md` (my `rm` is
  blocked by the permission classifier).

## Last session

Session of 2026-07-27. Plan 02 shipped as 3.5.3, `oversight-defects` as 3.5.4 (awaiting gated release
approval). This feature branched from master with the version-line bump folded in. Steps 1-3 done; scope
decisions taken with the user and recorded in `feature.md`. Awaiting confirmation before step 4.

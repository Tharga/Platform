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

- [x] **5. Public API rename in `Tharga.Team.Blazor`** — done. 993 tests green (up 4). Renamed
      `ThargaPlatformRegistration`/`ThargaPlatformOptions`/`AddThargaPlatform`/`UseThargaPlatform` to their
      Team names, and migrated every internal call site (sample + 5 test files) so the obsolete surface has
      no in-repo users.

  **Forwarding shape chosen: preserve every old name.** `ThargaPlatformCompatibility.cs` holds
  `[Obsolete] class ThargaPlatformOptions : ThargaTeamOptions` and an `[Obsolete] ThargaPlatformRegistration`
  carrying both old methods. To make the two entry points share one implementation rather than drift, the
  registration body was extracted into `internal AddThargaTeamCore(builder, options)`; the obsolete method
  builds the subclass and calls it. The simpler alternative (one options type, obsolete method taking
  `Action<ThargaTeamOptions>`) was rejected because it would silently drop the old *type* name from an
  additive release.
  - `ThargaPlatformCompatibilityTests` proves the alias is trustworthy, not merely compiling — including
    `AddThargaPlatform_RegistersTheSameServicesAsAddThargaTeam`, which compares the full registered service
    list of both paths. A forwarding alias that quietly did less would be worse than no alias.

  **Collision found: `Tharga.Team` already had a public `ThargaTeamRegistration`** whose
  `AddThargaTeam(this IServiceCollection)` has an **empty body** and is called nowhere. Two extension methods
  on different receivers do not clash at call sites, but the type names are ambiguous — which forced
  `TeamIconDialog`/`UserIconDialog` to fully-qualify `IconHttpClientName`. The real hazard is silent: a
  consumer calling `builder.Services.AddThargaTeam()` gets no error and no registrations.
  **User's decision: mark it `[Obsolete]` now, delete in 4.0** — keeps 3.6 strictly additive here while
  warning anyone who finds the wrong overload. The fully-qualified references must stay, since an obsolete
  type still participates in name resolution.

  Clean-build warning count is **20**, measured the way CI measures it (`grep -c " warning "`), against its
  threshold of 35. Worth watching: obsoleting a widely-used member is exactly what would breach that gate.
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

**`Tharga.Platform.Mcp` is not republished by this PR** — `build.yml` now packs seven projects and that is
not one of them. It freezes at its last published version (3.5.4 if that release is approved, else 3.5.3)
and stays installable. `Tharga.Team.Mcp` starts at **3.6.0** with no earlier history, so for a consumer this
is a package *swap*, not a version upgrade.

- [ ] **A. Merge + publish 3.6.0**, so `Tharga.Team.Mcp` exists on nuget.org. All seven packages take 3.6.0.
- [ ] **B. Deprecate `Tharga.Platform.Mcp`** on nuget.org — **only after A**, since the alternate package must
      already exist. Select *all* versions, reason "Other", alternate `Tharga.Team.Mcp`, and a message naming
      the two breaking changes (`AddPlatform` → `AddTeam`, `platform://` → `team://`). Deprecate, do **not**
      unlist — unlisting breaks restore for consumers who have not migrated.
- [ ] **C. Rename the GitHub repo** `Tharga/Platform` → `Tharga/Team` — **after A**, so 3.6.0's release run
      completes on a stable name and any failure is one change to debug, not two. GitHub redirects the old
      URL; I update the local remote afterwards.
- [ ] **D. Add the `team.tharga.net` DNS record**, verify it resolves, and only then retire
      `platform.tharga.net`. The `docs/CNAME` change lands in step 8, so do this around the same merge.
- [ ] **E. Optionally rename the local working directory** `C:\dev\tharga\Toolkit\Platform`. **The spec's
      warning about `settings.local.json` is out of date** — that file contains no `Platform` path today
      (verified). What it does orphan is the Claude session-memory and scratchpad directories, both keyed on
      the path. Cosmetic, do it whenever.
- [ ] **F. Rename references outside this repo** — cross-project, so they need explicit approval:
      the meta-repo `Toolkit/.claude/mission.md` (3 places: the sub-project table, the producing-project map,
      and the request-routing list), the two `Requests.md` headings `## Tharga.Platform` and
      `## Tharga.Platform — MCP`, and the `$DOC_ROOT` paths `Tharga/plans/Toolkit/Platform/` and
      `Tharga/Toolkit/Platform.md` (which this project's own `mission.md` points at).

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

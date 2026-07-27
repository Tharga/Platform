# Feature: Rename Platform to Team, remove the Platform concept

**Target release:** 3.6.0 (the version line moves from 3.5 in this same branch)
**Implements:** `planned/03-rename-platform-to-team.md`
**Supersedes:** issue #145, closed as "won't do" before this was requested; PR #154 (version-line bump, folded in here)

## Goal

The repo is named `Platform` but six of its seven packages are `Tharga.Team.*`. The odd one out is
`Tharga.Platform.Mcp`, whose every file is built on Team types — there is no Platform-level abstraction
underneath, so the name suggests a layer that does not exist. "Platform" also survives as a public API
concept (`AddThargaPlatform`, `ThargaPlatformOptions`, `ThargaPlatformRegistration`), in the docs site, the
sample project and the solution file.

The decision is to remove the concept entirely: the product is Team.

## Scope decisions taken with the user (2026-07-27)

These were **not** settled by the spec and change the shape of the work:

1. **MCP resource URIs are renamed outright**: `platform://…` becomes `team://…`, with no legacy alias.
   The spec assumed a non-breaking 3.7; the user chose the clean break instead, accepting that MCP clients
   referencing a `platform://` URI must be updated.
2. **The outward-facing steps are in scope** — GitHub repo rename, docs domain move, and NuGet deprecation
   of the old package ID. Each requires an action only the user can perform; they are tracked in `plan.md`
   as explicit hand-offs rather than silently omitted.

## Why the URI rename is a migration cost, not a forced break

`Tharga.Platform.Mcp` stays **published and deprecated**, not unlisted. Consumers pinned to it keep working
untouched. The break applies only when a consumer chooses to move to `Tharga.Team.Mcp`, and it is a
one-time, mechanical string change on their side. This must be stated plainly in the release notes —
a deprecation notice that hides a wire-contract change is worse than no notice.

## What changes

### Package
- `Tharga.Platform.Mcp` → **`Tharga.Team.Mcp`** (new NuGet ID; IDs cannot be renamed, so this is a new
  package plus a deprecation pointing at it). The other six keep their IDs.
- Types inside it lose the `Platform` prefix, and `IThargaMcpBuilder.AddPlatform()` becomes `AddTeam()`.
  Because the package ID is new, these are clean renames rather than forwarded obsoletes — a consumer
  moving package is already making a deliberate migration.
- **`platform://` → `team://`** for every resource URI.

### Public API (`Tharga.Team.Blazor`) — additive, obsolete-forwarded
- `AddThargaPlatform` → `AddThargaTeam`
- `ThargaPlatformOptions` → `ThargaTeamOptions`
- `ThargaPlatformRegistration` → `ThargaTeamRegistration`

These packages keep their IDs, so the old names must keep compiling with an `[Obsolete]` warning. They are
removed in 4.0.

### Repo, solution, sample
- `Tharga.Platform.sln` → `Tharga.Team.sln`; `Tharga.Platform.Sample` → `Tharga.Team.Sample`.
- GitHub repo `Tharga/Platform` → `Tharga/Team`, and the local remote updated.

### Docs
- `docs/docfx.json` `_appName` / `_appTitle` → `Tharga.Team`; new CNAME `team.tharga.net` with
  `platform.tharga.net` redirecting for a period.
- Sweep `docs/articles/**` and `README.md`. Leave "Platform" where it means the *family* of packages, which
  is legitimate — the README already lists `Tharga.Blazor`, which lives elsewhere.

## Acceptance criteria

- [ ] `Tharga.Team.Mcp` builds, packs and is published; `Tharga.Platform.Mcp` deprecated pointing at it.
- [ ] No `platform://` URI is served or documented; `team://` resolves for every previously-served resource.
- [ ] `AddThargaTeam` is the documented entry point; `AddThargaPlatform` still compiles, with an obsolete
      warning, and behaves identically.
- [ ] No `Tharga.Platform.*` project remains in the solution.
- [ ] Docs build and deploy under the new name; the old domain redirects.
- [ ] The version line produces **3.6.0**.
- [ ] Full test suite passes, with the MCP tests updated to the new URIs and type names.

## Out of scope

- Renaming `Tharga.Team.Service` or `Tharga.Team.MongoDB`. Rename when a name is *wrong*, not when a better
  one exists. `Platform` qualifies because the concept is being removed; `Service` is imprecise but true.
- Splitting the repo. Rejected in #145 for reasons that still hold: everything in the MCP bridge derives
  from `Tharga.Team`, so a split converts an in-repo reference into a cross-repo one.
- Renaming the local working directory `C:\dev\tharga\Toolkit\Platform` — a manual step that breaks absolute
  paths in `settings.local.json`.

## Risk

The obsolete-forwarding keeps the Blazor/Service surface non-breaking, but three things are externally
visible and must be sequenced deliberately: the repo rename, the docs domain change, and the new package ID.
Check DNS before removing the old CNAME. The MCP URI rename is a deliberate break and lives or dies on the
release notes being explicit.

# Mission: Tharga.Platform

Team management infrastructure: Tharga.Team, Tharga.Team.Service, Tharga.Team.MongoDB, Tharga.Team.Blazor.

- **Type**: Tool

## Git & Feature Workflow Overrides

These override the defaults in CLAUDE.md:

- **No develop branch** — `master` is the only long-lived branch
- **Feature branches start from origin/master:**
  1. `git fetch origin`
  2. `git checkout -b feature/<name> origin/master`
- **Feature close via PR** — do not merge locally. Instead:
  1. Push the branch
  2. Create a PR to `master` using `gh pr create`
  3. The PR description is used for **release notes** — write it at a level suitable for package consumers (what changed, why, how to use it). Avoid internal implementation details.
  4. The user reviews and merges on GitHub

## External References
- **Shared instructions**: `$DOC_ROOT/Tharga/shared-instructions.md`
- **Plan directory**: `$DOC_ROOT/Tharga/plans/Toolkit/Platform`
- **Backlog**: `$DOC_ROOT/Tharga/Toolkit/Platform.md`
- **Incoming requests**: `$DOC_ROOT/Tharga/Requests.md` — check sections "Tharga.Platform" and "Tharga.Platform — MCP" on startup

# Mission: Tharga.Team

Team management infrastructure: Tharga.Team, Tharga.Team.Service, Tharga.Team.MongoDB, Tharga.Team.Blazor.

- **Type**: Tool

- **CI**: GitHub Actions

## Workflow Overrides

- The PR description is used for **release notes** — write it at a level suitable for package consumers (what changed, why, how to use it). Avoid internal implementation details.

## Design Direction

The **target architecture** is defined in `$DOC_ROOT/Tharga/plans/Toolkit/Platform/architecture-v4.md`
(visual version: https://claude.ai/code/artifact/d94d3339-89b9-4080-b931-3fbfb42e7163). It is a goal to steer
by, **not a scheduled rewrite** — nothing in it is planned work.

**Read it before designing anything new**: a new service, a new package, a new contract, a new component, or
any change to authorization or persistence. Then:

- **Every change must move towards it or be neutral to it.** Never away from it.
- **Do not build on spec.** Adding structure that only serves the target — a port with one implementation, a
  client with no endpoints to call — repeats the mistake the target exists to correct. Take changes that are
  correct on their own terms today and happen to point the right way.
- **When a change would work against it, say so and propose the alternative** rather than proceeding.

The six rules, in short — the full statement and reasoning are in the document:

1. Operations, not CRUD, at every boundary.
2. One enforcement point: invariants and authorization live in the domain, nowhere else.
3. Contracts serialize by construction — no generic methods, no interface returns, no `IAsyncEnumerable`.
4. Ports speak the domain's language, never the store's.
5. The port expresses atomicity — any invariant spanning writes needs a transaction.
6. Claims are issued server-side and carry their provenance.

Concrete things that count as working against it: putting a rule where an untrusted caller could reach around
it; adding a second place that authorizes; giving a UI component a server-only dependency; inheriting a store
type into a contract or port; adding a package that quarantines no dependency and has one implementation.

**Revisit the stance** — clean-slate v4 rather than incremental — only if a consumer arrives with a hard date
for WebAssembly or a desktop client. Until then, incremental wins.

## External References
- **Shared instructions**: `$DOC_ROOT/Tharga/shared-instructions.md`
- **Target architecture**: `$DOC_ROOT/Tharga/plans/Toolkit/Platform/architecture-v4.md` — read before designing new surface; see Design Direction above
- **Plan directory**: `$DOC_ROOT/Tharga/plans/Toolkit/Platform`
- **Backlog**: `$DOC_ROOT/Tharga/Toolkit/Platform.md`
- **Incoming requests**: `$DOC_ROOT/Tharga/Requests.md` — check sections "Tharga.Platform" and "Tharga.Platform — MCP" on startup. Those headings still carry the **old** name: the file lives outside this repo, so it is renamed separately. Match on what is actually in the file, not on this project's name.
- **Eplicta requests**: `$DOC_ROOT/Eplicta/requests.md` — check for requests from Eplicta on startup

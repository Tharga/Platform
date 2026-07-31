# Plan: Audit actor provenance

Feature scope in `feature.md`. Tests run before each commit; `plan.md` is updated as each step lands.

Ordered so #163 — the part a consumer is waiting on — is complete and shippable at step 5, before the two
backlog items are folded in.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      *Done — `dotnet outdated` across the whole solution reports only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, deliberately held (4.0+ needs a paid Six Labors build-time licence). Nothing to
      apply.*

- [x] **2. Vocabulary + the honest default** (#163, first half)
      *Done — 6 tests, whole suite green (1074).*
      **The plan under-specified this.** It said "default to `Unknown`", but `AuditCallerType` had only
      `User` and `ApiKey` — there was no `Unknown` to default to. Added both `System` and `Unknown`, plus
      `AuditCallerSource.Background`.
      The rule is now *only positive evidence names an actor*: API-key scheme → `ApiKey`, web scheme →
      `User`, otherwise `User` only if the principal is actually authenticated, else `Unknown`. That last
      arm matters — an authenticated principal under an unrecognised scheme is still a person, so the
      change narrows to the case that was genuinely wrong rather than reclassifying every odd caller.
      `AuditCallerType.System`, `AuditCallerSource.Background`. Change `BuildEntry`'s ternary so a caller
      that is neither API nor web resolves to `Unknown` rather than `User`.
      **Tests first, and the important one is the negative:** no `HttpContext` must not produce `User`.
      That single assertion is the defect.

- [x] **3. `IAuditContextAccessor` — the ambient scope** (#163, second half)
      *Done — `AuditActor` record + `IAuditContextAccessor`/`AuditContextAccessor`, 9 tests covering
      await-survival, nesting, 20 concurrent flows, double-dispose, and both precedence directions.*
      **Precedence is narrower than "HTTP wins":** an **authenticated** principal wins, but an anonymous
      request does not — a job triggered through an unauthenticated endpoint still knows what it is, and
      "anonymous" is not a caller worth preferring over a declared one.
      `AuditActor.CorrelationId` also overrides the generated per-entry id, which is what lets a worker
      pull one job's entries back together — the grouping Eplicta asked for and cannot reconstruct later.
      `AsyncLocal`-backed, with a disposable scope: `using var _ = auditContext.Push(new AuditActor(...))`.
      `BuildEntry` falls back to it when there is no `HttpContext`; an HTTP principal always wins, so a
      stray scope cannot impersonate a real caller.
      Tests: the scope survives `await`; nested scopes restore the outer on dispose; concurrent async flows
      do not see each other's actor; an HTTP caller ignores an ambient scope entirely.

- [x] **4. Register it** — *Done: `TryAddSingleton` in `AddThargaAuditLogging`, unconditional and
      independent of storage mode; the accessor costs nothing until something pushes a scope. The worker
      pattern is documented in step 9 rather than here.*
      Registration alongside the other audit services, and the usage shape Eplicta needs — a scope per
      claimed job carrying service identity, team key and a per-job correlation id.

- [x] **5. Verify build + full suite, commit** — *Done: 1083 passed / 0 failed; build clean. #163 is closed and shippable from here.*
      #163 is closed and shippable from here.

- [~] **6. `CallerUserKey`** (backlog → Audit item 1)
      Field on `AuditEntry`, populated in `BuildEntry` from the resolved user; `AuditQuery` filter;
      `AuditPinnedFilter`; Mongo round-trip in `MongoDbAuditLogger` (both mapping directions — the entity
      and back). Then switch the per-user audit dialogs in `UsersListView` and `TeamComponent` from
      `CallerIdentity` substring matching to the stable key.
      **Check the CSV/JSON export and the grid** pick it up, the way `Metadata` had to be threaded through
      all four surfaces when it was added.

- [ ] **7. Caller filter in `AuditLogView`** (backlog → Audit item 2)
      Top-bar control, following the existing pattern: options drawn from inside the pinned scope, and
      hidden via `AuditFilterVisibility.ShouldShow` when it offers fewer than two choices.

- [ ] **8. Verify build + full suite, commit**

- [ ] **9. Documentation**
      The audit section of `implementation-guide.md` and `Tharga.Team.Service/README.md`: the actor model
      (who is recorded and how), the worker scope pattern with a code sample, and that `Unknown` now
      appears where `User` wrongly did. Separate `docs:` commit.

- [ ] **10. Version line 3.8 → 3.9**
      New public enum members, interface and field. Must land in this PR — the constant is hand-maintained.

- [ ] **11. Push and hand over for testing**
      Do **not** open the PR — the close-out commit must be last.

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`; close #163 with a summary; update Eplicta's watch entry so they can act; mark the
two backlog Audit items done; add the `## Follow-up` entry; archive `feature.md`; `git rm -r plan`; commit
`feat: audit-actor-provenance complete`; push; open the PR.

## Notes

- **The `AsyncLocal` scope is the part to get right.** A leaked or mis-restored scope would attribute one
  job's actions to another — the same class of wrongness this feature exists to remove, just harder to
  spot. Hence the concurrency and nesting tests in step 3 rather than a single happy-path one.
- **HTTP wins over the ambient scope, deliberately.** Otherwise a scope left open on a pooled thread could
  overwrite a real user's identity on a subsequent request.
- **Square icons is queued behind this** — its three design calls were settled 2026-07-31 (default on,
  transparent pad, square to the long side without upscaling) and recorded on the request in
  `Requests.md`, so it can start without further input.

## Last session

Branch created off master after 3.8.0 was released and published. Entra B2C support was dropped rather
than deferred — PlutusWave, its only requester, is moving to Entra.

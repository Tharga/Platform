# Plan: The support module, and Slack notifications

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid-licence reason. Everything else current.

## 2. Version

- [x] **`MAJOR_MINOR` stays `3.10`.** A new optional package is additive.

---

## 3. The package

- [x] `Tharga.Team.Support`, modelled on `Tharga.Team.Images` — the existing optional-package precedent.
- [x] References `Tharga.Team.Service` for the audit seam. **Nothing references it back**, so no consumer
      acquires Slack, email or AI by installing what they already had.
- [x] `AddThargaSupport()` — opt-in, and does nothing until a token *and* a channel are configured.
      **The signature changed during the work:** two optional lambdas (`configureSlack`,
      `configureNotifications`) meant `AddThargaSupport(o => o.DefaultChannel = ...)` bound silently to the
      Slack parameter and failed to compile with a misleading error. Replaced with one `SupportOptions`
      carrying `Slack` and `Notifications` sections — no ordering trap, and the shape that survives adding
      email and Jira.

## 4. The transport

- [x] `SlackClient` — `chat.postMessage` over `IHttpClientFactory` with a bearer token. No package.
- [x] Own folder and namespace, no `Tharga.Team.*` types crossing in — enforced by a reflection guard
      (`SlackNamespaceIsolationTests`) rather than a comment, because one convenience overload taking an
      `AuditEntry` would erode it silently. The guard carries two self-checks: that the scan finds the
      Slack types at all, and that the detector recognises a Team type when handed one.
- [x] A failing post never fails the operation. Posting runs on a background pump, so no HTTPS round trip
      touches the audited operation's thread.
- [x] **Slack reports its own failures with HTTP 200.** A bad token, an uninvited bot and a rate limit all
      arrive as `200 OK` with `{"ok":false}`, so the body is parsed. A status-code-only client would have
      called every one of those a successful post, and nobody would have learned the channel was silent.

## 5. Routing

- [x] Event → channel, message shaped per event, configured rather than coded. The routing key is
      `feature:action` — the same shape as a scope — with `team:*` and `*` wildcards.
- [x] **An event with no route is not sent.** The table is the allowlist; there is no second concept.
- [x] **Every matching route fires**, not just the first, so one event can reach two channels worded two
      ways. A `*` route beside a specific one therefore posts twice — visible in configuration, and the
      price of the fan-out being expressible at all.
- [x] Added beyond the spec: **`Success` on a route.** A failures-only channel is a natural want, and
      without it a success-worded route also narrates the times the operation threw.
- [x] **Per-event removal without a code change** — asserted through configuration alone.
- [x] A host raising its own event uses the same path. `IAuditEntryFactory.Create("invoice", "paid", ...)`
      routes on `invoice:paid` with no registration step.
- [x] **Default routes — and a finding that changed them.** The issue named *user logs on*, *user created*
      and *team created*. **Only the third exists.** The toolkit audits API-key authentication (`auth:*`)
      but has no interactive-logon event, and users are created as a side effect of first sign-in rather
      than through an audited call. The defaults are therefore `team:create`, `team:invite`,
      `team:remove-member` and `user:delete`, with a test asserting every built-in names an event the
      toolkit actually emits — a default naming something nothing raises looks configured and does
      nothing. The gap is documented in the README and the article. **Raising the two missing events is a
      separate decision:** it changes the audit stream for every consumer, so it is not folded in here.
- [x] Defaults name **no channel**, falling back to `DefaultChannel`. A default route cannot invent a
      channel name that happens to exist in someone's workspace.

## 6. Tests

**71 tests; the whole suite 1584 green, with no new warnings** (11 across the solution, all pre-existing
— the "baseline 8" in this plan counted library projects only; the other three are xUnit2031 in
`Blazor.Tests` and `Service.Tests`).

- [x] A routed event posts; an unrouted one does not.
- [x] Two events to different channels, and one event to two channels.
- [x] Removing a route stops the posts, with no code change — asserted through configuration.
- [x] A transport failure does not fail the triggering operation; nor does a transport that *throws*,
      which `ISlackClient` promises not to do but the sink does not rely on. One bad channel does not
      silence the others.
- [x] Message content per event — the assertion that would have failed had #129 not been delivered.
- [x] `Log()` does not post on the caller's thread, and what it queued is delivered once the pump runs.
- [x] **The container validates**, with `ValidateOnBuild` and `ValidateScopes`, plus a self-check proving
      a captive dependency really would break it.
- [x] **The sink and the hosted service are one instance.** Two would mean a queue nothing drains — the
      one wiring mistake that leaves every unit test green and every channel empty.
- [x] **The whole chain in one test:** an entry through `CompositeAuditLogger` comes out as a Slack post.
- [x] **The audit filter sits upstream of routing** — asserted, so it stays a known property rather than
      a production surprise.
- [x] **Mutation-checked: 10/10 caught.** It found a real defect on the first run — `IsTeamType` recursed
      through `GetGenericTypeDefinition()`, which returns itself on a definition, and stack-overflowed the
      test process (exit `0xC00000FD`). The run still printed `Passed! 68`, so the crash was the only
      signal that anything was wrong.

## 6b. The sample

- [x] Registered in `Tharga.Team.Sample` and **started**. It boots clean and stays silent with no token.
      The container-validation test builds a bare collection, and a bare collection is exactly what missed
      the captive dependency that stopped this sample once before.

## 7. Documentation

- [x] Package `README.md`, plus the root README (package table and dependency graph).
- [x] `docs/articles/notifications.md` — setup, routing, turning an event off, templates, own events,
      what is routable today, the two gotchas, and a troubleshooting table. Linked from `toc.yml` and
      `index.md`.
- [ ] Record in the spec: the packaging decision changed and why, that the AI bot is intended as a
      **Neurolito/Yggdrasil** bot rather than AI inside Team, and the missing logon / user-created events.
- [x] Separate `docs:` commit.

## 8. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] Archive to `$DOC_ROOT/.../done/support-slack-notifications.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Open, and worth deciding before it costs anything

- **#129 is delivered; `Requests.md` is stale.** `AuditMetadataKeys` carries the full vocabulary and the
  decorators populate it — `team:create` arrives with `team.name`, `team:invite` with `member.email`. The
  test asserts a message reading *"New team \*Acme\* created by alice."*, which is what the request asked
  for. **Action: mark #129 Done in `Requests.md`.**
- **Raise a logon and a user-created event?** Both were named in the issue and neither exists. Adding them
  changes the audit stream for every consumer, so it is the user's call rather than a fold-in.
- **Phase 3's inbound transport** — webhook with signature verification, or Socket Mode. Deferred, but it
  *"affects hosting requirements for every consuming product"*, and a Neurolito bot replying into a thread
  needs it.

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #187 merged. Package check unchanged.

**The packaging conclusion changed and the reasoning is recorded**, because a later reader will otherwise
find a spec saying "no new package" beside a package. The spec's test was right; email and
`Tharga.Neurolito.Client` are dependencies to quarantine, which is exactly what it said would justify one.

**2026-08-03.** §3–§7 done. Capability 1 is built: `Tharga.Team.Support` with a Slack transport, a routing
table and an audit sink; 71 tests, 10/10 mutations caught, registered in the sample and verified by
starting it.

**Three things the work changed from the plan**, each recorded above where it happened: the registration
signature (an ordering trap in two optional lambdas), the default routes (two of the three events the
issue named do not exist), and `Success` on a route (a failures channel is a real want, and without it a
success-worded route narrates failures).

**Next:** the spec write-up in §7, then §8 close-out when the user confirms the feature is done. Two
decisions are waiting: marking #129 Done in `Requests.md`, and whether to raise logon / user-created
audit events.

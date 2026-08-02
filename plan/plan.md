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

- [ ] `Tharga.Team.Support`, modelled on `Tharga.Team.Images` — the existing optional-package precedent.
- [ ] References `Tharga.Team.Service` for the audit seam. **Nothing references it back**, so no consumer
      acquires Slack, email or AI by installing what they already had.
- [ ] `AddThargaSupport()` — opt-in, and does nothing until routes are configured.

## 4. The transport

- [ ] `SlackClient` — outbound HTTPS POST with a bearer token, over `IHttpClientFactory`. No package.
- [ ] Own folder and namespace, **no `Tharga.Team.*` types crossing in**, so a future `Tharga/Slack` repo
      is a move rather than a rewrite. That instruction was right in the spec even though its packaging
      conclusion changed.
- [ ] A failing post must not fail the operation that triggered it. A notification is an observation, and
      an audit sink that throws would take the write down with it.

## 5. Routing

- [ ] Event → channel, message shaped per event, configured rather than coded.
- [ ] **An event with no route is not sent.** The table is the allowlist; there is no second concept.
- [ ] Default routes: the three events the issue named — user logs on, user created, team created — so it
      works on install rather than requiring configuration before anything happens.
- [ ] **Per-event removal without a code change**, which is the constraint the spec marks non-optional:
      `user logs on` is the one entry with real volume on a large tenant.
- [ ] A host raising its own event uses the same path — no second mechanism for custom events.

## 6. Tests

- [ ] A routed event posts; an unrouted one does not.
- [ ] Two events can go to different channels, which is the point of routing over an allowlist.
- [ ] Removing a route stops the posts, with no code change — asserted through configuration.
- [ ] A transport failure does not fail the triggering operation.
- [ ] The message content per event, so a channel does not just say "a DataChange occurred".
- [ ] Mutation-check each guard.

## 7. Documentation

- [ ] README: the package exists, what it is for, and that it is opt-in.
- [ ] `docs/`: routing, defaults, and how to remove an event.
- [ ] Record in the spec: the packaging decision changed and why, and that the AI bot is intended as a
      **Neurolito/Yggdrasil** bot rather than AI inside Team.
- [ ] Separate `docs:` commit before close-out.

## 8. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] Archive to `$DOC_ROOT/.../done/support-slack-notifications.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Open, and worth deciding before it costs anything

- **#129 is closed on GitHub but Pending in `Requests.md`.** The spec says resolve before phase 1: without
  operation metadata a Slack message says little more than *"a DataChange occurred"*. **Test 6.5 is where
  this bites** — if the metadata is not there, the message cannot be useful and the test will say so.
- **Phase 3's inbound transport** — webhook with signature verification, or Socket Mode. Deferred, but it
  *"affects hosting requirements for every consuming product"*, and a Neurolito bot replying into a thread
  needs it.

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #187 merged. Package check unchanged.

**The packaging conclusion changed and the reasoning is recorded**, because a later reader will otherwise
find a spec saying "no new package" beside a package. The spec's test was right; email and
`Tharga.Neurolito.Client` are dependencies to quarantine, which is exactly what it said would justify one.

**Next:** §3.

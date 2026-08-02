# Feature: The support module, and its first capability — Slack notifications

**Branch:** `feature/support-slack-notifications` (off `master`)
**Started:** 2026-08-02
**Release:** **`MAJOR_MINOR` stays `3.10`.** Purely additive — a new optional package nobody installs by
accident.

Issue #142, phase 1. Spec: `$DOC_ROOT/Tharga/plans/Toolkit/Platform/planned/04-notifications-and-support.md`.

## The packaging decision — changed from the spec, and why

The spec says **no new package**, reasoning that *"Slack needs no third-party dependency, so there is
nothing to quarantine, and a package with one consumer and no quarantine fails the packaging test."* That
was correct for its inputs. **The inputs changed** (user, 2026-08-02): the support module is to handle
email in and out, Jira tickets, customer-facing ticket views, AI chat and human chat.

- **Email breaks the no-dependency premise.** Receiving mail needs a real dependency, and AI via
  `Tharga.Neurolito.Client` is a second. Neither belongs in `Tharga.Team.Service`, which every consumer
  installs.
- **`Tharga.Team.Images` is the precedent** — an optional package existing solely to quarantine ImageSharp.

So: **`Tharga.Team.Support`**, created now rather than when the first dependency lands. Building the Slack
sink in `Tharga.Team.Service` and moving it later costs a namespace move *and* a package addition — a
breaking change for anyone who has configured it. Nothing has adopted it yet; it is free today and not
free later.

**`Tharga.Team.*` rather than `Tharga.Support`**, because it depends on users, teams and authorization.
That is honest about the coupling, and matches `Mcp` / `Entra` / `Images`.

**No UI package yet.** The customer portal and agent view may want `Tharga.Team.Support.Blazor`, but there
is no UI in this feature and splitting on spec is the mistake the architecture doc names.

## Feature one — routing, not just an allowlist

The user's ask is *"hook on different events and send different messages to specific channels"*. That is
**routing**; the spec settled only *which* events reach Slack. A routing table subsumes the allowlist — an
event with no route does not go — and satisfies both constraints the spec marks non-optional:

- **Per-event, not all-or-nothing.** `user logs on` is the one entry with real volume on a large tenant.
- **Removing an event must never be a code change.**

## Scope

1. `Tharga.Team.Support` package, registered by an opt-in `AddThargaSupport()`.
2. `SlackClient` — outbound HTTPS POST with a bearer token. **No third-party package**; this one really
   does need nothing.
3. A sink on the existing audit seam — `CompositeAuditLogger` already fans out and applies filtering and
   enrichment first, so notifications are a fourth sink rather than a new mechanism.
4. **Routing**: event → channel, with the message shaped per event. Configurable without a code change.
5. Consumers can raise their own events through the same path.

## Not in scope

Phases 2–5: support cases, Slack inbound, the AI bot, Jira. The AI bot is intended to be a
**Neurolito/Yggdrasil bot** (user, 2026-08-02) rather than AI inside Team — recorded in the spec so it is
not re-derived later.

## Acceptance criteria

1. A registered Slack sink receives routed audit events and posts them.
2. Routing is per-event and configurable; removing an event is configuration, not code.
3. An event with no route is not sent — the routing table *is* the allowlist.
4. Consumers can raise their own events through the same path.
5. `Tharga.Team.Service` gains no new dependency, and nothing installs Slack by accident.
6. Full suite green; no new warnings (baseline 8).

## Done condition

All six met, docs on both surfaces, `MAJOR_MINOR` still `3.10`, `plan/` removed in the close-out commit,
PR open against `master`.

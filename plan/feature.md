# Feature: Eplicta 3.8.3 defect sweep (#175, #177)

## Goal

Fix two of the three defects Eplicta filed against 3.8.3 on 2026-08-01 — the authorization one and the
options-forwarding one.

## Scope decision: two, not three

This started as all three (#175, #176, #177) on the argument that each records the same "action when done" in
`$DOC_ROOT/Eplicta/requests.md` — *upgrade `Tharga.Team.*`*, then delete a local workaround — so three
releases would make Eplicta upgrade three times.

**Rescoped to two, deliberately, when #175 and #177 were done and #176 was not yet started.** #175 is tier 1
under `mission.md` and shipping it promptly beats bundling; #176 is explicitly non-blocking in Eplicta's own
note (*"Interim: register the sender by hand — this does not block EP-4498"*). #176 keeps its own branch and
PR. The cost is accepted and named: Eplicta clears two workarounds now and the email one later.

Each defect was verified live in the code before starting — not taken on the report alone.

| Issue | Defect | Tier | Eplicta's workaround this removes |
|---|---|---|---|
| **#175** | `AuditLogView.TeamKey` scopes the query but not the access decision | **1 — authorization** | Passing both `TeamKey` **and** `PinnedFilter` in `Features/Audit/Audit.razor` purely to satisfy the gate |
| **#177** | `IconOptions.MaxUploadBytes` / `MaxDimension` not forwarded | 2 — Eplicta | A standalone `Configure<IconOptions>` after `AddThargaTeamBlazor` in `Program.cs` |

## Scope

**#175** — resolve the effective team at the gate as `query.TeamKey ?? PinnedFilter?.TeamKey ?? TeamKey` in
`AuditLogView.QueryAsync`, so every call site benefits rather than only the probe. The gate probe at
`OnInitializedAsync` currently builds `new AuditQuery { Take = 1 }` with no team, which is why a host passing
`TeamKey` alone falls to the system-scope branch and is refused.

**#177** — forward the whole `IconOptions` instance in `RegisterIcons` rather than two named properties.
Copying by default and naming exceptions is the correction `ThargaBlazorOptionsForwarder` already applies one
layer up, and its own XML docs cite this issue as the same failure on the same path.

## Acceptance criteria

- [x] `<AuditLogView TeamKey="@teamKey" />` alone authorizes a caller holding that team's `audit:read`.
- [x] Pinning still wins over the parameter, and the system-scope branch still works when no team is named.
- [x] All four `IconOptions` properties reach the container from both the facade and granular paths, and a
      property added later cannot silently fail to forward.
- [x] The facade path keeps working unchanged for both fixes.
- [x] Regression tests for each, including one that would have caught #177's whole class of bug.
- [x] Full solution builds with no new warnings and the whole suite passes.

## Done condition

#175 and #177 closable, and the icon options path can no longer silently drop a property.

## Carried forward

**#176 — granular setup cannot register the email sender.** Not started. Design already settled: add `Email`
and `AddEmailService<T>()` to `ThargaBlazorOptions` and register the three-way choice (custom sender > SMTP
options > nothing) in `AddThargaTeamBlazor`, with the facade forwarding its own `ThargaTeamOptions.Email`
down. **Follows the #157 precedent** — icons were folded into `AddThargaTeamBlazor` rather than given a
standalone `AddThargaTeamIcons`, and the facade forwards `o.Icon = options.Icon` explicitly for backwards
compatibility. A standalone `AddThargaTeamEmail` was the alternative, rejected because it leaves the two
paths free to drift again, which is the defect being fixed.

Also carried: a documentation point Eplicta raised on #176. `ITeamEmailSender` is described as the
abstraction for "team-related emails" but has only `SendInviteAsync`. State explicitly that invitations are
the only mail the toolkit sends, so a consumer can decide whether to adapt an existing pipeline or configure
SMTP. The granular setup section also does not mention that email needs separate wiring.

**Tidy-up, not urgent:** `ThargaBlazorOptionsForwarder` now duplicates the new generic `OptionsForwarder`. It
was left alone because it carries its own `NotForwarded` contract and a test bound to its API, so folding it
in is a cleanup with no consumer benefit.

## Out of scope

- #155 (role badges on the profile page) and #142 (Slack/support panel) — both tier 3.
- The claims-path cache work, merged as PR #198.

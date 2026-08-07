# Feature: Eplicta 3.8.3 defect sweep (#175, #176, #177)

## Goal

Fix the three defects Eplicta filed against 3.8.3 on 2026-08-01, so one upgrade clears all three of their
local workarounds.

## Why these three together

All three are Eplicta reports of the same vintage, and each records the same "action when done" in
`$DOC_ROOT/Eplicta/requests.md`: *upgrade `Tharga.Team.*`*, then delete a local workaround. Three separate
releases would make them upgrade three times to clear three workarounds filed on the same day.

Each was verified live in the code before starting — not taken on the report alone.

| Issue | Defect | Tier | Eplicta's workaround this removes |
|---|---|---|---|
| **#175** | `AuditLogView.TeamKey` scopes the query but not the access decision | **1 — authorization** | Passing both `TeamKey` **and** `PinnedFilter` in `Features/Audit/Audit.razor` purely to satisfy the gate |
| **#177** | `IconOptions.MaxUploadBytes` / `MaxDimension` not forwarded | 2 — Eplicta | A standalone `Configure<IconOptions>` after `AddThargaTeamBlazor` in `Program.cs` |
| **#176** | Granular setup cannot register the email sender | 2 — Eplicta | Registering `SmtpTeamEmailSender` by hand |

#175 is tier 1 under `mission.md`: it refuses access a team Owner legitimately holds. It goes in first so it
is shippable on its own if #176 needs discussion.

## Scope

**#175** — resolve the effective team at the gate as `query.TeamKey ?? PinnedFilter?.TeamKey ?? TeamKey` in
`AuditLogView.QueryAsync`, so every call site benefits rather than only the probe. The gate probe at
`OnInitializedAsync` currently builds `new AuditQuery { Take = 1 }` with no team, which is why a host passing
`TeamKey` alone falls to the system-scope branch and is refused.

**#177** — forward the whole `IconOptions` instance in `RegisterIcons` rather than two named properties.
Copying by default and naming exceptions is the correction `ThargaBlazorOptionsForwarder` already applies one
layer up, and its own XML docs cite this issue as the same failure on the same path.

**#176** — add `Email` and `AddEmailService<T>()` to `ThargaBlazorOptions` and register the three-way choice
(custom sender > SMTP options > nothing) in `AddThargaTeamBlazor`, with the facade forwarding its own
`ThargaTeamOptions.Email` down. **This follows the #157 precedent** — icons were folded into
`AddThargaTeamBlazor` rather than given a standalone `AddThargaTeamIcons`, and the facade forwards
`o.Icon = options.Icon` explicitly for backwards compatibility. A standalone `AddThargaTeamEmail` was the
alternative; rejected because it leaves the facade and granular paths free to drift again, which is the
defect being fixed.

Also in #176, a documentation point Eplicta raised: `ITeamEmailSender` is described as the abstraction for
"team-related emails" but has only `SendInviteAsync`. Say explicitly that invitations are the only mail the
toolkit sends, so a consumer can decide whether to adapt an existing pipeline or configure SMTP.

## Acceptance criteria

- [ ] `<AuditLogView TeamKey="@teamKey" />` alone authorizes a caller holding that team's `audit:read`.
- [ ] Pinning still wins over the parameter, and the system-scope branch still works when no team is named.
- [ ] All four `IconOptions` properties reach the container from both the facade and granular paths, and a
      property added later cannot silently fail to forward.
- [ ] A granular host can register a custom `ITeamEmailSender`, or SMTP via `Email`, or neither.
- [ ] The facade path keeps working unchanged for all three.
- [ ] Regression tests for each, including one that would have caught #177's class of bug.
- [ ] Full solution builds with no new warnings and the whole suite passes.

## Done condition

All three issues closable, Eplicta can drop three workarounds in one upgrade, and the granular path is no
longer a step behind the facade for icons or email.

## Out of scope

- #155 (role badges on the profile page) and #142 (Slack/support panel) — both tier 3.
- The claims-path cache work, which is PR #198 on its own branch.

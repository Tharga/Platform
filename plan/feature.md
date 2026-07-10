# Feature: Host override for the "Create team" action

**GitHub issue:** [Tharga/Platform#123](https://github.com/Tharga/Platform/issues/123)
**Branch:** `feature/create-team-override`
**Package:** Tharga.Team.Blazor

## Goal

Let a host app intercept or redirect the built-in "Create team" action in
`Tharga.Team.Blazor`, so a teamless user's create flow can launch the host's own
onboarding wizard (e.g. FortDocs `/get-started`) instead of the bare create — and
without disabling the programmatic create API (the current `AllowTeamCreation = false`
footgun that also blocks `CreateTeamAsync`).

Additive and non-breaking: when a host wires nothing, behavior is exactly as today.

## Design

Three-tier precedence at each built-in "Create team" entry point:

1. `CreateTeamRequested` `EventCallback` parameter (per component) — if the host wired
   it, invoke it and skip the built-in create.
2. `ThargaBlazorOptions.CreateTeamPath` (global option) — else if set, navigate there.
3. Built-in behavior (unchanged) — else the `/team` link (TeamSelector) or
   `ITeamManagementService.CreateTeamAsync()` (TeamComponent).

**Out of scope (explicit):** no `TeamCreated` service event. The host owns onboarding on
its own wizard page/callback; `AutoCreateFirstTeam`-created teams will not trigger host
onboarding, which is accepted.

## Entry points affected

- `TeamSelector.razor` — the teamless "Create team" `RadzenLink` → `/team`.
- `TeamComponent.razor` — the "Create new Team" button → `CreateTeamAsync()` (gated by
  `AllowTeamCreation`; that visibility gate is preserved).

## Scope

**In scope**
- `ThargaBlazorOptions.CreateTeamPath` option.
- `CreateTeamRequested` `EventCallback` parameter on `TeamSelector` and `TeamComponent`.
- Precedence wiring at both entry points.
- Tests (reflection smoke tests + bUnit render assertions where feasible).
- Docs: `Tharga.Team.Blazor/README.md` + `docs/articles/`.
- Optional sample demonstration.

**Out of scope**
- `TeamCreated` / post-create service event.
- Any change to `AllowTeamCreation` semantics.
- RenderFragment/template replacement of the create UI.

## Acceptance criteria

- [ ] `ThargaBlazorOptions.CreateTeamPath` exists (nullable, XML-documented).
- [ ] `TeamSelector` and `TeamComponent` each expose a `CreateTeamRequested`
      `EventCallback` `[Parameter]`.
- [ ] Precedence holds at both entry points: callback > `CreateTeamPath` > built-in.
- [ ] With nothing wired, behavior is unchanged (link → `/team`, button → `CreateTeamAsync`).
- [ ] New tests cover the parameters/option and the precedence; full suite green
      (`dotnet build -c Release`, `dotnet test -c Release`).
- [ ] README + docs updated with a host-onboarding example.

## Done condition

All acceptance criteria met, full test suite green, docs updated, user has tested from
the pushed branch and confirmed, close-out commit lands (plan/ removed) and PR opened
to `master`.

## Versioning

Additive / non-breaking → suggested minor bump to **3.2.0**. Actual version is CI-driven
(csproj stays `1.0.0`).

## Consumer follow-up (after merge)

- Move the Eplicta `requests.md` "Watching" entry for Platform#123 to a follow-up:
  FortDocs wires its `/get-started` onboarding wizard to the `TeamSelector` "Create team"
  entry (via `CreateTeamPath` or `CreateTeamRequested`), replacing the dashboard-embed-only
  steering. Relates to EP-4685 / P0.5.

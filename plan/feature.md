# Feature: Service-level authorization for team operations

## Goal
Make the toolkit's team operations enforce authorization in the **service layer** — not just the Blazor
UI — so any caller (the Blazor circuit **or** a consumer's REST controller calling the same services) is
protected by the correct scope / option / ownership checks. **No controllers are added to the toolkit**;
consumers own their REST surface and rely on these service-level checks.

## Problem (today)
- `[RequireScope]` on `ITeamManagementService` is **inert** — `ScopeProxy` is never wired
  (`AddScopedWithScopes` is never called).
- Only Rename/Delete have an in-method `AssureAccessLevel(Administrator)` — **membership-based**, so it
  excludes team **API keys**.
- **Consent** and **member display-name** edits have **no** server-side check at all.
- Net: enforcement is UI-shaped (button visibility). Fine for a Blazor-only app; unsafe the moment the
  services are exposed via REST.

## Settled authorization model
| Operation | Allowed when |
|---|---|
| **Create** | authenticated **AND** `AllowTeamCreation` |
| **Delete** | (`team:manage` **AND** own team **AND** `AllowTeamCreation`) **OR** `teams:delete` |
| **Rename** | `team:manage` (own team) |
| **Consent** | `team:manage` (own team) |
| **Member invite/remove/role** | `member:manage` (own team) |
| **Member display name** | `member:manage` (own team) |
| **Transfer ownership** | Owner-only (unchanged) |

Rules:
- **In-team scopes** (`team:read`, `team:manage`, `member:manage`) authorize only the caller's **own team**:
  the caller's `TeamKey` claim must equal the operation's `teamKey` argument (closes the "admin of A acts on
  B" hole). Works uniformly for member users **and** team API keys (both carry the scope claims).
- **`teams:delete`** (NEW system scope, toolkit-defined, consumer-applied): **unconditional** delete of
  **any** team — bypasses membership **and** `AllowTeamCreation`.
- **`AllowTeamCreation`** = self-service guardrail for the `team:manage` tier (create + in-team delete),
  enforced **in the service**. Does not affect `teams:delete`, rename, or consent.
- **Create** has no scope (self-service); owner = the current user.

## Scope / option changes
- Add **`teams:delete`** (system scope) — registered as a built-in system scope.
- Move **member display-name** edit: `team:manage` → `member:manage`.
- Give **consent** real `team:manage` enforcement (currently none).
- Drop the inert/misleading `team:manage` requirement from **Create**.
- Update the `team:manage` description (it no longer covers create or member-name).
- Enforce **`AllowTeamCreation`** in the service (create + in-team delete branch).

## Key implementation notes / risks
- **Enforcement mechanism:** the existing `ScopeProxy` is claim-only — it requires a `TeamKey` claim but
  does **not** bind it to the `teamKey` argument, and can't express the `teams:delete` bypass or the
  `AllowTeamCreation` option. So we need an authorization layer that reads the caller's claims (via
  `ITeamPrincipalAccessor`) and applies the table above per operation. Likely a small
  `ITeamAuthorizer` injected into the service, or a dedicated authorization decorator in
  `Tharga.Team.Service`. (Decide in step 2.)
- **Layering:** `TeamServiceBase` lives in `Tharga.Team` (core); `ITeamPrincipalAccessor` / `ScopeProxy`
  live in `Tharga.Team.Service`. The authorizer must read the principal, so enforcement belongs in the
  **Service layer** (decorator over `ITeamService`/`ITeamManagementService`), not in core `TeamServiceBase`.
- **UI stays a convenience layer:** TeamComponent gates remain for UX, but the service checks are the
  real backstop (and what protects consumer controllers).

## Out of scope
- REST controllers / Swagger — consumer's responsibility.
- Other `teams:*` system operations (e.g. `teams:read` cross-tenant listing) — future, same pattern.
- API create with an arbitrary owner (system-key-as-creator) — create stays self-service (owner = current user).

## Acceptance criteria
- [ ] Each operation enforces the model at the **service** layer, verified across caller types: team-admin
      **user**, team **API key**, **`teams:delete`** holder, unauthorized caller, **cross-team** attempt,
      and `AllowTeamCreation` on/off.
- [ ] `teams:delete` registered as a built-in system scope; bypasses membership + `AllowTeamCreation`.
- [ ] In-team scopes bound to `TeamKey == teamKey` (admin of A cannot act on B).
- [ ] `AllowTeamCreation` enforced in the service for create + the in-team delete branch.
- [ ] Member display-name gated by `member:manage`; consent gated by `team:manage`.
- [ ] Create requires no scope (self-service); owner = current user.
- [ ] Blazor UI still works; service checks are the backstop.
- [ ] Tests cover the authorization matrix; full suite green on net9.0/net10.0.
- [ ] Docs updated (model, `teams:delete`, `AllowTeamCreation` semantics, REST-consumer guidance).

## Done condition
PR `feature/team-service-authorization` → `master`; user confirms. Breaking (enforcement now fires +
`TeamKey` binding + scope move) → **3.2.0**.

## Sequencing note
The unmerged `feature/scopeview-system-scopes` branch also edits the `team:manage` description line.
Recommend finalizing that branch first, then rebasing this onto the updated master to avoid a (trivial)
one-line conflict — and so this feature's description change builds on it.

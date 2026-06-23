# Plan: Service-level authorization for team operations (→ 3.2.0)

- [ ] 0. NuGet up front — `dotnet outdated -u`; verify build + tests.
- [ ] 1. **Scopes/options surface**
       - Add `TeamScopes`-adjacent constant for `teams:delete` (system) — e.g. `SystemTeamScopes.Delete = "teams:delete"`.
       - Register `teams:delete` as a built-in system scope (in the default registration, into `ISystemScopeRegistry`).
       - Update `team:manage` description (drop create + member-name); fold member display-name into the `member:manage` description.
- [ ] 2. **Authorization mechanism** — design + build the service-layer authorizer.
       - Reads the caller principal via `ITeamPrincipalAccessor`; helpers: `HasScope(scope)`, `TeamKeyMatches(teamKey)`, `HasSystemScope(scope)`.
       - Decide shape: injected `ITeamAuthorizer` used inside the service, **or** an authorization decorator over `ITeamManagementService` in `Tharga.Team.Service`. (Lives in the Service layer — it needs the principal.)
- [ ] 3. **Apply per-operation rules** (the model table):
       - Create: authenticated + `AllowTeamCreation` (no scope); owner = current user.
       - Delete: (`team:manage` + `TeamKey==teamKey` + `AllowTeamCreation`) OR `teams:delete`.
       - Rename / Consent: `team:manage` + `TeamKey==teamKey` (consent gains enforcement).
       - Member invite/remove/role + member display-name: `member:manage` + `TeamKey==teamKey` (name moved off team:manage).
       - Transfer ownership: Owner-only (unchanged).
       - Replace the membership-based `AssureAccessLevel` on rename/delete with the claim+TeamKey checks (so API keys work).
- [ ] 4. **Enforce `AllowTeamCreation` in the service** (create + in-team delete branch); `teams:delete` exempt.
- [ ] 5. **Update `[RequireScope]` attributes** on `ITeamManagementService` to match (remove from Create; member-name → member:manage) — kept as documentation even though enforcement now runs through the authorizer.
- [ ] 6. **UI** — align `TeamComponent` gates (member display-name → `member:manage`); keep the buttons as a UX layer over the enforced service.
- [ ] 7. **Tests** — authorization matrix: operation × caller (admin user / team API key / `teams:delete` holder / unauthorized) × `AllowTeamCreation` on-off × cross-team attempt. Plus registration test for the `teams:delete` system scope.
- [ ] 8. **Docs** — scopes table (+ `teams:delete`), the authorization model, `AllowTeamCreation` semantics, and explicit guidance that REST consumers rely on these service checks (no toolkit controllers).
- [ ] 9. Build + full test run; finalize.

## Notes
- **Sequencing:** finalize `feature/scopeview-system-scopes` first, then rebase this on updated master
  (one-line overlap on the `team:manage` description; ScopeView's `ShowSystemScopes` table is otherwise independent).
- **No controllers** in the toolkit. Consumers wire REST and rely on these service checks.
- **Deferred:** API create with arbitrary owner; `teams:read` cross-tenant listing (same `teams:*` pattern).

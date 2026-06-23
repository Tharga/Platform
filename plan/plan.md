# Plan: Service-level authorization for team operations (→ 3.2.0)

- [ ] 0. NuGet up front — `dotnet outdated -u`; verify build + tests.
- [~] 1. **Scopes/options surface**
       - [x] Add `SystemTeamScopes.Delete = "teams:delete"` (Tharga.Team).
       - [ ] Register `teams:delete` as a built-in system scope (ThargaBlazorRegistration → `ISystemScopeRegistry`). DEFERRED — touches ThargaBlazorRegistration; do after ScopeView #110 merges (rebase first).
       - [ ] Update `team:manage` description (drop create + member-name); fold member display-name into `member:manage`. DEFERRED — same file overlaps ScopeView #110.
- [x] 2. **Authorization mechanism** — DONE. Decided: an **authorization decorator over `ITeamService`** in `Tharga.Team.Service` (mirrors `AuditingTeamServiceDecorator`; wired via the existing `DecorateWith` pattern, outermost so it checks before audit). Built `TeamAuthorizer` (reads principal via `ITeamPrincipalAccessor`): `IsAuthenticatedAsync`, `HasTeamScopeAsync(scope, teamKey)` (scope claim **and** `TeamKey == teamKey`), `HasSystemScopeAsync(scope)`. 8 unit tests green.
- [x] 3. **Apply per-operation rules** — DONE in `AuthorizationTeamServiceDecorator` (wired outermost). Removed `AssureAccessLevel` from base rename/delete (decorator is sole gate; API keys now work). 17 decorator matrix tests.
       - Create: authenticated + `AllowTeamCreation` (no scope); owner = current user.
       - Delete: (`team:manage` + `TeamKey==teamKey` + `AllowTeamCreation`) OR `teams:delete`.
       - Rename / Consent: `team:manage` + `TeamKey==teamKey` (consent gains enforcement).
       - Member invite/remove/role + member display-name: `member:manage` + `TeamKey==teamKey` (name moved off team:manage).
       - Transfer ownership: Owner-only (unchanged).
       - Replace the membership-based `AssureAccessLevel` on rename/delete with the claim+TeamKey checks (so API keys work).
- [x] 4. **Enforce `AllowTeamCreation` in the service** — DONE (decorator `RequireCreateAsync` / `RequireDeleteAsync` read `TeamLifecycleOptions.AllowTeamCreation`, set from `o.AllowTeamCreation` at registration); `teams:delete` exempt.
- [x] 5. **Update `[RequireScope]` attributes** — DONE. Removed from `CreateTeamAsync` (now XML-documented as AllowTeamCreation + auth); `SetMemberNameAsync` → `MemberManage`; `DeleteTeamAsync` doc notes the `teams:delete` path; interface summary points to the service decorator as the enforcement.
- [x] 6. **UI** — DONE. `TeamComponent` member display-name edit gate `_canManage` → `_canManageMembers` (matches the `member:manage` service rule; self-edit unchanged).
- [ ] 7. **Tests** — authorization matrix: operation × caller (admin user / team API key / `teams:delete` holder / unauthorized) × `AllowTeamCreation` on-off × cross-team attempt. Plus registration test for the `teams:delete` system scope.
- [ ] 8. **Docs** — scopes table (+ `teams:delete`), the authorization model, `AllowTeamCreation` semantics, and explicit guidance that REST consumers rely on these service checks (no toolkit controllers).
- [ ] 9. Build + full test run; finalize.

## Notes
- **Sequencing:** finalize `feature/scopeview-system-scopes` first, then rebase this on updated master
  (one-line overlap on the `team:manage` description; ScopeView's `ShowSystemScopes` table is otherwise independent).
- **No controllers** in the toolkit. Consumers wire REST and rely on these service checks.
- **Deferred:** API create with arbitrary owner; `teams:read` cross-tenant listing (same `teams:*` pattern).

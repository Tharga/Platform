# Plan: Move every first-level surface onto the gated read path

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-01, before the branch was cut.** Unchanged: `SixLabors.ImageSharp`
      3.1.12 → 4.0.0 held for the paid-licence reason. Everything else current.

---

## 2. Make the gated read path actually gate  `[~]`

### The finding that comes before everything else

**`ITeamManagementService`'s reads are not enforced.** All four declare
`[RequireScope(TeamScopes.Read)]`:

```csharp
[RequireScope(TeamScopes.Read)] Task<ITeam<TMember>> GetTeamAsync<TMember>(string teamKey);
[RequireScope(TeamScopes.Read)] Task<ITeam> GetTeamByKeyAsync(string teamKey);
[RequireScope(TeamScopes.Read)] IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey);
[RequireScope(TeamScopes.Read)] Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey);
```

…and the implementations are **plain pass-throughs to `_inner`**:

```csharp
public Task<ITeam<T>> GetTeamAsync<T>(string teamKey) => _inner.GetTeamAsync<T>(teamKey);
```

The attributes are inert because the service is registered with a bare
`services.AddScoped(typeof(ITeamManagementService), …)` — **no `ScopeProxy`**. `shared-instructions.md`
describes the proxy as failing closed on an unattributed method; that guarantee only holds where the
proxy is installed, and here it is not.

**Mutations are safe by accident**: they delegate to `ITeamService`, which `AuthorizationTeamServiceDecorator`
wraps. The decorator gates mutations and `GetAllTeamsAsync` — it does **not** gate reads. So the reads are
checked nowhere.

**Consequence for this feature:** moving surfaces onto `ITeamManagementService.GetTeamAsync` as it stands
would relocate the hole and make it *look* fixed. Worse than leaving it, because the next reviewer sees
`[RequireScope]` on the interface and concludes the read is checked.

**This also revises what 3.8.2 shipped.** It delivered the *shape* of a gated read path — interfaces,
attributes, a filtered `ITeamDirectoryService` that genuinely does filter — but not the enforcement on 4
of its 5 read members. The roadmap's "the gated read path exists, surfaces have not moved onto it" was
too generous.

### Work

- [ ] **Decide how to enforce**: install `ScopeProxy` on the `ITeamManagementService` registration (via
      `AddTeamService<,>`, the mechanism built for this), or enforce inline in `TeamManagementService`.
      The proxy is the designed answer and would also cover future members; inline is narrower and
      cannot fail closed on a method someone forgets to attribute.
- [ ] **Check every member is attributed before installing the proxy** — it throws on an unattributed
      method, which is the desired behaviour but will surface anything missed as a runtime failure.
- [ ] Tests: a caller without `team:read` is refused each of the four reads; a Viewer-level member is not.

### Then the five gaps in the surface

- [ ] **`GetAllTeamsAsync`** — wholly-system-wide gated home (decision taken: new interface, precedent
      `ISystemApiKeyManagementService`).
- [ ] **`SetTeamConsentAsync`** on `ITeamManagementService`.
- [ ] **`AssignOwnerAsync`** on `ITeamManagementService` — added to `ITeamService` only in the previous
      feature; the exact drift this feature exists to stop.
- [ ] **`GetInvitationAsync(inviteCode)`** — new, authorized by the code rather than a scope.
- [x] **`SelectTeamEvent` — resolved, no new contract needed.** `TeamStateService` is internal framework
      code that already injects `ITeamService`, which the rule permits (the ban is on components,
      controllers and MCP providers). It can bridge the event, leaving `TeamSelector` on
      `ITeamStateService` alone.

## 3. Move the surfaces

One commit per surface where practical, so a regression bisects cleanly.

- [ ] `TeamDialog` and `InviteUserDialog` first — one call each, both already gated. Cheapest proof the
      approach works.
- [ ] `UsersListView`, `TeamsListView` — reads plus the two cross-team calls.
- [ ] `TeamSelector` — depends on the `SelectTeamEvent` decision.
- [ ] `TeamComponent` — the largest, four distinct calls.
- [ ] `TeamInviteView` — depends on `GetInvitationAsync`.
- [ ] MCP `TeamResourceProvider` / `TeamUserResourceProvider`.
- [ ] `AccessPage` in the sample — **decision required**.

**Check after each:** a Viewer-level member still sees exactly what they saw before. Acceptance
criterion 2 is what makes this safe for every consumer not using `Custom`, and it is easy to break
silently.

---

## 4. Guards

- [ ] **Architecture test**: no component, dialog or MCP provider injects `ITeamService`. Must fail
      *naming the offending type*, or the next person to add a surface learns nothing from it.
- [ ] `[EditorBrowsable(Never)]` on `ITeamService` — **decision required**.
- [ ] XML docs on `ITeamService` naming it the host's implementation contract, not for injection.

---

## 5. Documentation

- [ ] An **"inject this, not that"** table in the Service README and the implementation guide. Consumers
      do not read architecture rationale; they do read "which type do I inject".
- [ ] State the three categories once: **gated** (`[RequireScope]`, all-or-nothing), **filtered**
      (`ITeamDirectoryService` — no attribute, recomputes per item), **internal** (`ITeamService`).
- [ ] A release note for the behaviour change: a caller lacking `team:read` starts being refused.

---

## 6. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings, **sample runs**.
- [ ] **`MAJOR_MINOR` → `3.10`.** Behaviour-changing.
- [ ] Archive to `$DOC_ROOT/.../done/gated-team-reads.md`; move `07-move-surfaces-to-gated-reads.md` out
      of `planned/`.
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-01 (setup).** Branch cut off `master` after 3.9.x merged. Package check unchanged. Plan
written, **awaiting confirmation and three decisions before any code changes.**

**The finding that shaped this plan:** the gated surface is incomplete. Five members that first-level
surfaces call have no gated equivalent, including `GetAllTeamsAsync`, which cannot go on
`ITeamManagementService` without breaking the wholly-team-bound rule that makes its registration valid.
Step 2 therefore comes before any migration.

**Next:** settle the three decisions, then step 2.

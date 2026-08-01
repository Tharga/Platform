# Plan: Move every first-level surface onto the gated read path

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-01, before the branch was cut.** Unchanged: `SixLabors.ImageSharp`
      3.1.12 → 4.0.0 held for the paid-licence reason. Everything else current.

---

## 2. Make the gated read path actually gate  `[~]`  — enforcement DONE 2026-08-01

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

- [x] **Decided (user, 2026-08-01): enforce inline.** The proxy was rejected on cost — `AddTeamService<,>`
      is generic-only while the implementation is closed at runtime via `MakeGenericType`, and
      `GetTeamAsync<TMember>` is a generic method through `DispatchProxy`. Neither risk is worth taking
      for four method bodies.
- [x] All four reads enforce `team:read` via `RequireTeamReadAsync`. 12 tests.
- [x] **Also split the feature (user, 2026-08-01).** This PR makes the path real and is
      **behaviour-neutral** — no first-level surface calls these reads yet. Moving the surfaces is a
      separate PR where behaviour actually changes. `MAJOR_MINOR` stays at `3.9` here.

**The check reads the caller's own membership, not the roster.** `GetTeamMemberAsync(teamKey, user.Key)`
carries the access level, tenant roles and scope overrides the decision needs, at one lookup instead of
loading every member on each read.

**Two escapes, deliberately different:**
- **No `IScopeRegistry` or no `IUserService`** → skip the check. The application does not use scopes at
  all, and enforcing would refuse reads it never gated. This is the one-argument constructor path.
- **A resolved caller who is null** → **refuse**. Identity could not be established, which is not a state
  in which to serve team data. Same rule as the self-delete guard.

**The class doc was actively misleading and is corrected.** It read *"Scope enforcement is handled by
`ScopeProxy<T>` in Tharga.Team.Service"* — which is how four unenforced reads survived review. It now
states where each half is enforced and that the `[RequireScope]` attributes are documentation, not
mechanism.

### Then the five gaps in the surface

- [x] **`ITeamOversightService`** — `GetAllTeamsAsync` in both overloads. Its own interface because it is
      **wholly system-wide**: a no-argument cross-team read on the wholly-team-bound
      `ITeamManagementService` would break the invariant that makes one scope registration true of every
      method, not merely sit oddly beside it. Enforcement is downstream on `SystemTeamScopes.Read`, where
      it already was.
- [x] **`SetTeamConsentAsync`** on `ITeamManagementService`, gated on the in-team `team:manage` — consent
      is a team's own statement about what it exposes, so an operator overriding it via a system grant
      would be a much larger claim than fixing a typo in a name.
- [x] **`AssignOwnerAsync`** on `ITeamManagementService`. Its scope is a *system* grant, unlike every
      other member — noted in the XML docs so the attribute is not read as the whole rule.
- [x] **`ITeamInvitationService.GetInvitationAsync(inviteCode)`** — its own interface, authorized by the
      **code** rather than a scope. An invitee holds nothing for the team they are joining, so requiring
      a scope would make an invitation impossible to accept.

**The invitation lookup is a real narrowing, not a relocation.** The old pattern decoded the code, read
the *entire team* by key, and matched the roster in memory — returning every member, access level and
membership state to someone who had only been sent a link. `GetInvitationAsync` returns four fields.

**Malformed, unknown and already-used all return null.** Distinguishing them would confirm whether a
team exists to an unauthenticated visitor holding a guessed link.

**This is also why the invitation screen could not simply move onto the gated path** — and it is the
break that PR 2 would otherwise have discovered at runtime: with enforcement live, an invitee calling
`GetTeamAsync` is refused.
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

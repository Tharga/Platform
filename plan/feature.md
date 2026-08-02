# Feature: Complete the team-service registration

**Branch:** `fix/complete-team-service-registration` (off `master`)
**Started:** 2026-08-02
**Release:** **`MAJOR_MINOR` stays `3.10`** — releases as `3.10.2`. Purely additive: hosts that already
hand-wire the facets keep working, because the fix uses `TryAdd`.

**Source:** PlutusWave, 2026-08-02, **High** — *"it has now broken a host's startup twice, and the next
one lands in production."*

## The reported problem

Upgrading 3.8.3 → 3.10.0 stopped PlutusWave booting:

```
Unable to resolve service for type 'Tharga.Team.ITeamDirectoryService'
while attempting to activate 'Tharga.Team.Mcp.TeamUserResourceProvider'.
```

3.10.0 split what a caller injects into five interfaces over `ITeamService`. Nothing registered the new
ones, so the host had to forward all five by hand — five identical lines differing only in a type
argument. It had already happened once at 3.5.2 with `ITeamLifecycleService`. **Twice makes it a pattern.**

## The actual cause is narrower, and worse

**The toolkit already registers all five facets.** They sit inside `if (o._memberType != null)` in
`ThargaBlazorRegistration`, which is only true when the host used the **three-argument** overload:

```csharp
o.Blazor.RegisterTeamService<TeamService, UserService, TeamMember>();   // registers all five
o.Blazor.RegisterTeamService<TeamService, UserService>();               // registers none of them
```

PlutusWave used the two-argument one. So this is not "the toolkit forgot to register new interfaces" —
it is **one overload silently doing far less than the other**, with no signal which you picked.

**And it is why the sample never caught it:** `Tharga.Team.Sample/Program.cs` uses the three-argument
overload, so every facet resolves in-repo and the broken path is never exercised.

## Why it evaded everything (PlutusWave's analysis, and it is right)

1. **Semver-invisible.** Adding an interface breaks no signature; an API diff lists the three new
   interfaces as *additions* — new capabilities, not new obligations.
2. **Environment-dependent.** `ValidateOnBuild` is on by default only in Development, so a host whose
   integration tests boot elsewhere stays green while the app cannot start. Their 288 tests passed.
3. **Invisible to container validation for components.** A Blazor `@inject` resolves at *render* time, so
   a missing facet surfaces as a 500 on the page nobody opened until production.

## Scope

1. **Infer the member type** when the two-argument overload is used, by walking the registered service's
   base chain for a generic base carrying an `ITeamMember` argument. `TeamServiceRepositoryBase<TEntity,
   TMember>` — what every MongoDB host derives from — carries it, so the common case needs no host change
   at all. This addresses the cause rather than the symptom.
2. **`TryAdd` semantics** for all facets, so a host substituting or decorating one still wins. That is the
   single legitimate reason the registration was left manual, and `TryAdd` answers it.
3. **A startup completeness check** that names any facet still unregistered — mirroring
   `UserServiceCompletenessCheck`, which already exists for the *user* half of exactly this problem. Logs
   an error by default; `ThrowOnIncompleteTeamService` for hosts that want it fatal.

Inference cannot always succeed: a host deriving straight from `TeamServiceBase` with no generic member
type has nothing to infer from. **That is precisely the case the check has to name**, rather than leaving
a render-time failure in production.

## Not in scope

**"One options surface for Team, shared by Blazor and MCP"** (PlutusWave, same day, Medium) is a design
change, and their own note says it should not stall behind this one: *"that one is a cheap bug-class fix"*.

## Acceptance criteria

1. The two-argument overload registers every facet the three-argument one does, when the member type is
   inferable.
2. A host that hand-registers a facet keeps its own — `TryAdd`, not `Add`.
3. When inference fails, startup **names the missing interfaces**; it does not fail at render time.
4. The check logs by default and throws only when the host opts in.
5. A test proves the two overloads produce the same resolvable set — the assertion that would have caught
   this originally.
6. Full suite green; no new warnings (baseline 8).

## Done condition

All six met, docs on both surfaces, `MAJOR_MINOR` still `3.10`, `plan/` removed in the close-out commit,
PR open against `master`, and the request marked Done in `Requests.md`.

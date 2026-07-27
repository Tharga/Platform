# Feature: Authorization defects — escalation and claim provenance

Implements `planned/02-authorization-defects.md`. Two live holes, neither reported by a consumer, both
affecting running systems.

## Goal

Ownership can only change through `TransferOwnershipAsync`, and a scope granted by a system role can no longer
satisfy a check that asks for it on a specific team.

## Defect 1 — ownership is changeable through a primitive

`TeamServiceBase.SetMemberRoleAsync` applies the requested access level with no invariant of its own:

```csharp
public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
{
    await SetTeamMemberRoleAsync(teamKey, userKey, accessLevel);   // no guard
    ...
}
```

The authorization decorator gates it on `member:manage` and nothing more. Two consequences, in opposite
directions:

- **Escalation** — a `member:manage` holder sets themselves to `Owner` and takes the team, bypassing
  `TransferOwnershipAsync`'s "only the current owner can transfer" check entirely. In the sample, team
  Administrators hold `member:manage`.
- **Ownerless team** — the same caller demotes the existing Owner. `RemoveMemberAsync` explicitly refuses to
  remove an owner ("Transfer ownership first"), but nothing stops demoting one. A team with no owner cannot
  then transfer ownership, because transfer requires the caller to *be* the owner.

The invariant exists; it just lives above a primitive that is independently reachable. That is target rule 1.

**The fix is clean because transfer does not use the public method.** `TransferOwnershipAsync` calls the
protected `SetTeamMemberRoleAsync` directly (`TeamServiceBase.cs:344`), so the public method can refuse both
directions without breaking transfer. First-owner assignment also bypasses it — teams are created with their
owner already set.

## Defect 2 — scope claims carry no provenance

`TeamMembershipClaimsBuilder` (from access level) and `TeamServerClaimsTransformation` (from system roles) both
emit `TeamClaimTypes.Scope`. Nothing downstream distinguishes them, so **a system role silently satisfies a
team-scope check**.

- `audit:read` is registered at `AccessLevel.Administrator` *and* mapped to a system role. An unpinned
  `AuditLogView` queries every team, so a team Administrator of one team can read the whole system's audit log.
- `apikey:manage` has the same shape: a Developer at `AccessLevel.User` manages that team's API keys,
  bypassing the Administrator requirement the team registry declares.

`ApiKeyScopes.SystemManage` escapes only because it happens to be registered solely as a system scope. The
mechanism does not prevent the collision.

## Acceptance criteria

- [ ] `SetMemberRoleAsync` refuses to grant `Owner`.
- [ ] `SetMemberRoleAsync` refuses to demote the current `Owner`.
- [ ] `TransferOwnershipAsync` still promotes and demotes correctly.
- [ ] Creating a team still assigns its first owner.
- [ ] A scope granted by a system role is distinguishable from one granted by team access level.
- [ ] A team Administrator holding `audit:read` from their access level cannot read another team's entries.
- [ ] The sample grants the Developer role only system scopes.
- [ ] Full test suite passes.

## Done condition

Both defects closed with tests that fail against the current code, docs updated where the scope model is
described, and the migration noted in the PR description.

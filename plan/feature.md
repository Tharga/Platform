# Feature: cache the team custom-roles read

## Goal

Remove the per-request database read that `TeamServerClaimsTransformation` performs when a host has
enabled dynamic tenant roles.

## Background

`TeamServerClaimsTransformation` is an `IClaimsTransformation`, so it runs once per authenticating HTTP
request. Two of the three reads it performs are already served from process-wide static caches:

| Read | Cache |
|---|---|
| `IUserService.GetCurrentUserAsync` | `UserServiceBase._userCache` |
| `ITeamService.GetTeamMemberAsync` | `TeamServiceBase._teamMemberCache` |
| effective scopes | **uncached** |

The third has no cache. When `AddThargaDynamicTenantRoles()` is registered, `TeamGrantResolver` prefers
`ITenantRoleService`, and `TenantRoleService.GetEffectiveScopesAsync` calls
`ITeamService.GetTeamCustomRolesAsync`, which reads the whole team document via
`TeamServiceBase.GetTeamAsync`. Every authenticated request therefore reads the full team document
purely to union in custom-role scopes.

## Scope

Cache **`GetTeamCustomRolesAsync`** only — keyed by team key, in the same shape as the two existing
caches (static `ConcurrentDictionary`, no TTL, invalidated on write).

**Explicitly not caching `GetTeamAsync`.** The team document carries the member roster, and four paths
read the team specifically to get complete, uncached state — `TeamServiceBase.cs:248-251` says so
outright, because the cached member path filters on `State == Member` and cannot tell an invitee from a
stranger. Those paths make access decisions off `team.Members`: `TransferOwnershipAsync` and
`AssignOwnerAsync` check for `AccessLevel.Owner`, `SetMemberSuspendedAsync` and `RemoveMemberAsync`
check membership state. A cache there needs ~15 invalidation sites, several fronting authorization
guards, and one miss is an authorization defect rather than a stale label. A full-team cache would also
be churned by `SetTeamMemberLastSeenAsync`, which `TeamStateService.SetSelectedTeamAsync` calls on
every team selection.

Custom roles by contrast have exactly one write path and are read by nothing that authorizes.

## Acceptance criteria

- [ ] A repeated `GetTeamCustomRolesAsync` for the same team reads the store once.
- [ ] `SetTeamCustomRolesAsync` drops the entry, so the next read reflects the write.
- [ ] `DeleteTeamAsync` and `CreateTeamAsync` drop the entry, so a recycled team key cannot inherit a
      previous team's roles.
- [ ] A null or empty team key does not touch the cache and behaves as it does today.
- [ ] `ITenantRoleService.GetEffectiveScopesAsync` performs no team read on a warm cache.
- [ ] Full solution builds and the whole test suite passes.

## Done condition

The claims path performs zero database reads per request in the dynamic-tenant-roles configuration,
with invalidation covered by tests, and no cache placed in front of a roster read.

## Out of scope

- Adding a TTL to any cache. The two existing caches have none; introducing one for only the third
  would be inconsistent. If TTLs are wanted they should be a separate decision across all three.
- The uncached `GetConsentedTeamsAsync` query on the non-member consent path. Different shape (keyed by
  role set, not team) and a separate decision.
- GitHub issues #175, #176 and #177 — the Eplicta defect sweep, which is its own branch.

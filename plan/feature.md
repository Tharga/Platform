# Feature: User upsert race fix

GitHub issue: [Tharga/Platform#65](https://github.com/Tharga/Platform/issues/65)

## Goal

Close the check-then-act race in `UserServiceRepositoryBase.GetUserAsync` that produces duplicate `UserEntity` rows when two near-simultaneous first-time logins for the same identity hit a fresh per-environment User collection. The race surfaces as two user-visible failures across every Tharga.Team consumer:

1. `NavMenu` / any path through `UserServiceBase.GetUserAsync` throws `InvalidOperationException: Sequence contains more than one element` (from `SingleOrDefaultAsync` in the underlying `_collection.GetOneAsync`).
2. The Tharga.Team.Blazor `/developer/user` admin page crashes — RadzenDataGrid rejects two rows with the same Key in the render tree.

Quilt4Net.Server has shipped downstream mitigations (catch-and-recover in `UserService.GetUserAsync`, a `MongoWriteException`/`DuplicateKey` catch on the same path, and a manual `UserIdentityIndexHostedService` that creates the unique index out-of-band) — but none of those prevent the *creating* race. The upstream fix landing here removes the need for every consumer to carry its own workaround.

## Scope

Three coordinated pieces, all in `Tharga.Team.MongoDB`:

1. **Unique index on `User.Identity`** declared on `UserRepositoryCollection.Indices` so Tharga.MongoDB's `AssureIndex` creates and maintains it. After this, MongoDB rejects the racing `AddAsync` with a `MongoWriteException` of category `DuplicateKey`.
2. **Race-proof `GetUserAsync`** in `UserServiceRepositoryBase`: catch `MongoWriteException` with `WriteError.Category == ServerErrorCategory.DuplicateKey` from `AddAsync`, re-read by Identity, and return the winning row. The unique index is the load-bearing safety net; the catch is the recovery path. (Approach selected per user direction over `FindOneAndUpdate $setOnInsert`; both close the race, this one is significantly simpler.)
3. **Consumer-facing index extension point.** Today `UserRepositoryCollection<TUserEntity>` is `internal` and consumers cannot subclass it to declare per-deployment indices. Promote it to `public`; the `Indices` member is already `override` on the base. Add a new `RegisterUserRepository<TUserEntity, TCollection>()` overload to `ThargaTeamOptions` that accepts an explicit collection-type override. The default `RegisterUserRepository<TUserEntity>()` continues to register the built-in collection class.

## Behaviour

- **No data migration runs as part of this fix.** Existing deployments that already have duplicate User rows must dedupe before deploying — Tharga.MongoDB's `AssureIndex` will surface the conflict in startup logs and refuse to create the index until the data is clean. This is called out in the PR description so admins see it. (Decision per user direction over a built-in dedupe-on-startup migration.)
- The race fix is correctness-only — no behaviour change for non-racing callers. Consumers do not need to touch their `UserService` override after upgrading (Quilt4Net.Server's downstream mitigations become no-ops once the upstream is in place; they can be removed in a separate consumer-side bump).

## Acceptance criteria

1. `UserRepositoryCollection.Indices` declares a unique index on `Identity`.
2. `UserServiceRepositoryBase.GetUserAsync` catches `MongoWriteException` with `DuplicateKey` category from `AddAsync`, re-reads by Identity, returns the winning row.
3. `UserRepositoryCollection<TUserEntity>` is `public`; `Indices` can be overridden by a consumer subclass.
4. `ThargaTeamOptions.RegisterUserRepository<TUserEntity, TCollection>()` overload exists; the DI registration in `ThargaTeamRegistration.AddThargaTeamRepository` uses the configured collection type.
5. New unit tests: (a) duplicate-key add → recovery returns the winning row from a fake repository, (b) the new `RegisterUserRepository<TUserEntity, TCollection>` overload plumbs through to DI (the registered `IUserRepositoryCollection<>` resolves to the consumer's subclass), (c) `UserRepositoryCollection<>.Indices` contains the expected `Identity` unique index.
6. `dotnet build -c Release` and `dotnet test -c Release` both green.

## Done condition

PR opened from `feature/user-upsert-race-fix` → `master`, all CI checks green, user has confirmed. Once the next `Tharga.Team.*` release is consumed in Quilt4Net.Server, its `UserIdentityIndexHostedService` + `MongoWriteException` catch in `UserService.GetUserAsync` can be removed (tracked separately, not part of this feature).

## Out of scope

- `FindOneAndUpdate $setOnInsert` upsert path — both approaches close the race; catch-DuplicateKey is the chosen minimal change.
- Built-in dedupe migration — admins clean up existing duplicates manually. The Tharga.MongoDB `AssureIndex` startup error is the surface.
- Symmetric fix for `TeamRepository` user-key races — separate scope; the Team collection already has `UniqueTeamMemberKey` index per `TeamRepositoryCollection.Indices`.
- Framework package bumps (Microsoft.AspNetCore.* 9.0.15 → 10.0.7) — deferred, separate PR.
- Consumer-side cleanup in Quilt4Net.Server — separate downstream bump after this lands.

# Plan: User upsert race fix

## Steps

- [x] **1. Unique index on `User.Identity`**
  - Override `Indices` on `UserRepositoryCollection<TUserEntity>` (currently inherits the empty default from `DiskRepositoryCollectionBase<>`):
    ```csharp
    public override IEnumerable<CreateIndexModel<TUserEntity>> Indices =>
    [
        new(Builders<TUserEntity>.IndexKeys.Ascending(x => x.Identity),
            new CreateIndexOptions { Unique = true, Name = "Identity" })
    ];
    ```
  - Verify `IUser.Identity` is a property reachable via `x => x.Identity` (it is — defined on `IUser` in `Tharga.Team/IUser.cs`).

- [x] **2. Catch-DuplicateKey recovery in `UserServiceRepositoryBase.GetUserAsync`**
  - In `Tharga.Team.MongoDB/UserServiceRepositoryBase.cs`, wrap the `AddAsync` call:
    ```csharp
    protected override async Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal)
    {
        var identity = claimsPrincipal.GetIdentity().Identity;

        var user = await _userRepository.GetAsync(identity);
        if (user != null) return user;

        var candidate = await CreateUserEntityAsync(claimsPrincipal, identity);
        try
        {
            await _userRepository.AddAsync(candidate);
            return candidate;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Lost the race; another request inserted first. Return the winning row.
            return await _userRepository.GetAsync(identity);
        }
    }
    ```
  - `MongoDB.Driver` is already a transitive dependency via `Tharga.MongoDB` — no new package reference needed.

- [x] **3. Promote `UserRepositoryCollection<TUserEntity>` to `public`**
  - Change the class declaration from `internal class` to `public class`.
  - `Indices` is already `override` on the base type → already virtual; consumers can subclass and override.

- [x] **4. `RegisterUserRepository<TUserEntity, TCollection>` overload in `ThargaTeamOptions`**
  - Add an internal `Type _userCollectionType` field (defaults to null).
  - Existing `RegisterUserRepository<TUserEntity>()` continues to set `_userEntity = typeof(TUserEntity)` and leaves `_userCollectionType = null` (registration falls back to the built-in collection class).
  - New overload:
    ```csharp
    public void RegisterUserRepository<TUserEntity, TCollection>()
        where TUserEntity : EntityBase, IUser
        where TCollection : UserRepositoryCollection<TUserEntity>
    {
        _userEntity = typeof(TUserEntity);
        _userCollectionType = typeof(TCollection);
    }
    ```
  - In `ThargaTeamRegistration.AddThargaTeamRepository`, the `userRepositoryCollectionImplementationType` becomes `o._userCollectionType ?? typeof(UserRepositoryCollection<>).MakeGenericType(userEntityType)`.

- [x] **5. Tests** — New `Tharga.Team.MongoDB.Tests` project created with 7 tests (4 race-recovery + 2 registration + 1 index declaration). All passing.
  - `UserServiceRepositoryBase`/`GetUserAsync` race-recovery test: build a `TestUserService` derivative whose `IUserRepository<>` is a fake that throws `MongoWriteException` (DuplicateKey) on first `AddAsync` and returns a known winning row on subsequent `GetAsync`. Assert the service returns the winning row, not the candidate. Construct `MongoWriteException` via its public constructor (or reflection if the `WriteError`/`Category` fields are read-only) — verify the reachable surface first.
  - `RegisterUserRepository<TUserEntity, TCollection>` plumbing test: register a derived collection type via the new overload, build an `IServiceProvider`, resolve `IUserRepositoryCollection<TUserEntity>`, assert the runtime type is the consumer subclass.
  - `UserRepositoryCollection.Indices` declaration test: instantiate a `UserRepositoryCollection<TestUserEntity>`, read `Indices`, assert one entry with `Unique = true`, `Name = "Identity"`, and the key path is `Identity`.
  - Existing `Tharga.Team.Blazor.Tests` test `UserServiceBaseDefaultsTests` already covers `UserServiceBase.GetCurrentUserAsync` cache behaviour — confirm it still passes.

- [x] **6. Build + test** — solution builds clean; 296 / 296 tests passing (289 before + 7 new).
  - `dotnet build c:/dev/tharga/Toolkit/Platform/Tharga.Platform.sln -c Release` — 0 warnings, 0 errors.
  - `dotnet test c:/dev/tharga/Toolkit/Platform/Tharga.Platform.sln -c Release` — full suite green.

- [x] **7. README check** — `Tharga.Team.MongoDB/README.md` updated with a "Adding per-deployment User indices" section showing the subclass + `RegisterUserRepository<TUserEntity, TCollection>` pattern.
  - Per shared-instructions, skip README updates if the feature has no consumer-visible surface. This feature does add a public extension point (`UserRepositoryCollection<>` is now subclassable, new `RegisterUserRepository<TUserEntity, TCollection>` overload). Decide whether `Tharga.Team.MongoDB/README.md` should mention the new extension point briefly. The migration prerequisite (dedupe before deploy) goes in the PR description, not the README.

- [ ] **8. Commit + push the feature branch**
  - Conventional commit prefix: `fix:` — this resolves a bug.
  - Suggested message: `fix: race-proof user creation + unique Identity index + collection extension point`.

- [ ] **9. Pause for user verification** (per the principle: plan/ stays on the feature branch through Completing; deleted in the close-out commit before PR opens).

## Open questions

(none — design choices locked via `AskUserQuestion` during planning)

## Last session
2026-05-11 — All implementation steps complete. 7 new tests in a new `Tharga.Team.MongoDB.Tests` project; 296 / 296 passing. README extension-point section added. Ready for commit + push + user verification.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.MongoDB;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// Verifies the consumer-facing index extension point added under Tharga/Platform#65:
/// <see cref="ThargaTeamOptions.RegisterUserRepository{TUserEntity, TCollection}"/> registers a
/// subclass of <see cref="UserRepositoryCollection{TUserEntity}"/> as the implementation of
/// <see cref="IUserRepositoryCollection{TUserEntity}"/>, so consumers can declare per-deployment indices.
///
/// We inspect the <see cref="ServiceDescriptor"/> on the registered <see cref="IServiceCollection"/>
/// rather than building the provider, because the underlying <c>DiskRepositoryCollectionBase</c>
/// ctor casts the injected factory to a concrete type that a test substitute can't satisfy.
/// </summary>
public class RegisterUserRepositoryTests
{
    [Fact]
    public void RegisterUserRepository_Default_Overload_Registers_Builtin_Collection()
    {
        var services = new ServiceCollection();
        services.AddThargaTeamRepository(o => o.RegisterUserRepository<UserServiceRepositoryBaseRaceTests.TestUserEntity>());

        var descriptor = services.Single(s =>
            s.ServiceType == typeof(IUserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>));

        Assert.Equal(
            typeof(UserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>),
            descriptor.ImplementationType);
    }

    [Fact]
    public void RegisterUserRepository_Subclass_Overload_Registers_Consumer_Subclass()
    {
        var services = new ServiceCollection();
        services.AddThargaTeamRepository(o =>
            o.RegisterUserRepository<UserServiceRepositoryBaseRaceTests.TestUserEntity, CustomCollection>());

        var descriptor = services.Single(s =>
            s.ServiceType == typeof(IUserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>));

        Assert.Equal(typeof(CustomCollection), descriptor.ImplementationType);
    }

    public class CustomCollection : UserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>
    {
        public CustomCollection(IMongoDbServiceFactory mongoDbServiceFactory, ILogger<UserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>> logger, IOptions<ThargaTeamOptions> options = null)
            : base(mongoDbServiceFactory, logger, options) { }
    }
}

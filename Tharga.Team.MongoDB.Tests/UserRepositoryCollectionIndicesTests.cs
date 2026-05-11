using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// Verifies that <see cref="UserRepositoryCollection{TUserEntity}.Indices"/> declares the
/// unique <c>Identity</c> index added under Tharga/Platform#65.
///
/// The base <c>DiskRepositoryCollectionBase</c> ctor casts the injected factory to a concrete
/// <c>MongoDbService</c>, which an NSubstitute proxy can't satisfy. Since the override is purely
/// declarative and doesn't depend on instance state, we bypass the ctor via
/// <see cref="RuntimeHelpers.GetUninitializedObject"/>.
/// </summary>
public class UserRepositoryCollectionIndicesTests
{
    [Fact]
    public void Indices_Includes_Unique_Identity_Index()
    {
        var collection = (UserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>)
            RuntimeHelpers.GetUninitializedObject(typeof(UserRepositoryCollection<UserServiceRepositoryBaseRaceTests.TestUserEntity>));

        var indices = collection.Indices.ToArray();

        var identityIndex = Assert.Single(indices);
        Assert.Equal("Identity", identityIndex.Options.Name);
        Assert.True(identityIndex.Options.Unique);

        var keyDoc = identityIndex.Keys.Render(new RenderArgs<UserServiceRepositoryBaseRaceTests.TestUserEntity>(
            BsonSerializer.SerializerRegistry.GetSerializer<UserServiceRepositoryBaseRaceTests.TestUserEntity>(),
            BsonSerializer.SerializerRegistry));
        Assert.True(keyDoc.Contains("Identity"));
        Assert.Equal(1, keyDoc["Identity"].AsInt32);
    }
}

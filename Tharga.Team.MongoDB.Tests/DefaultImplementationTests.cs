using System.Reflection;
using Tharga.Team.MongoDB;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// The shipped defaults: a host wanting standard behaviour writes no storage types at all.
/// </summary>
/// <remarks>
/// Before these existed, that host wrote five types — a member, a team entity, a team service, a user
/// entity and a user service — of which only two methods contained anything a host could reasonably want
/// to decide. The rest was ceremony, and the sample proved it: every body was an object initializer
/// copying its own arguments.
/// </remarks>
public class DefaultImplementationTests
{
    /// <summary>
    /// The optional properties are opt-in <i>by shape</i>: the toolkit persists them only when the entity
    /// declares somewhere to put them. A default missing one would disable a documented feature silently,
    /// with no error to explain why — so the choice to declare all three is pinned here rather than left
    /// to whoever next edits the type.
    /// </summary>
    [Theory]
    [InlineData(nameof(IUser.DirectoryId))]
    [InlineData(nameof(IUser.LastSeen))]
    [InlineData(nameof(IUser.Icon))]
    public void DefaultUserEntity_DeclaresEveryOptionalProperty(string propertyName)
    {
        var property = typeof(DefaultUserEntity).GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotNull(property);
    }

    [Fact]
    public void DefaultUserEntity_IsAUser()
    {
        Assert.True(typeof(IUser).IsAssignableFrom(typeof(DefaultUserEntity)));
        Assert.False(typeof(DefaultUserEntity).IsAbstract);
    }

    [Fact]
    public void DefaultTeamTypes_AreConcreteAndUsable()
    {
        Assert.False(typeof(DefaultTeamMember).IsAbstract);
        Assert.False(typeof(DefaultTeamEntity).IsAbstract);
        Assert.True(typeof(ITeamMember).IsAssignableFrom(typeof(DefaultTeamMember)));
        Assert.True(typeof(ITeam).IsAssignableFrom(typeof(DefaultTeamEntity)));
    }

    /// <summary>
    /// The member type is discoverable from the default service, which is what lets the two-argument
    /// <c>RegisterTeamService</c> wire everything without the host naming a type.
    /// </summary>
    [Fact]
    public void TheDefaultTeamService_YieldsItsMemberTypeToInference()
    {
        Assert.Equal(typeof(DefaultTeamMember), TeamMemberTypeResolver.Resolve(typeof(DefaultTeamService)));
    }

    /// <summary>
    /// Both factories stay overridable. They are the only members the base leaves abstract, and a host
    /// deriving from the default must be able to change what they build without abandoning it.
    /// </summary>
    [Theory]
    [InlineData("CreateTeam")]
    [InlineData("CreateTeamMember")]
    public void TheDefaultTeamService_KeepsItsFactoriesOverridable(string methodName)
    {
        var method = typeof(DefaultTeamService).GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsFinal);
    }

    /// <summary>
    /// Key generation is the one genuine decision in building a user, so it is its own virtual member
    /// rather than buried inside the factory.
    /// </summary>
    [Fact]
    public void TheDefaultUserService_KeepsKeyGenerationOverridable()
    {
        var method = typeof(DefaultUserService).GetMethod("GenerateUserKey",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsFinal);
    }

    /// <summary>
    /// Two generated keys differ. A default that handed every user the same key would be a data-losing
    /// bug rather than a cosmetic one, and nothing else in the model would notice.
    /// </summary>
    [Fact]
    public void GeneratedUserKeys_AreNotAllTheSame()
    {
        var method = typeof(DefaultUserService).GetMethod("GenerateUserKey",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var instance = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(DefaultUserService));
        var keys = Enumerable.Range(0, 20).Select(_ => (string)method.Invoke(instance, null)).ToArray();

        Assert.All(keys, k => Assert.False(string.IsNullOrWhiteSpace(k)));
        Assert.True(keys.Distinct().Count() > 1);
    }
}

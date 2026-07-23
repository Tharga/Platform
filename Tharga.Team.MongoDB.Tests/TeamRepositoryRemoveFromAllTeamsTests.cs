using System.Linq.Expressions;
using MongoDB.Driver;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// Verifies <see cref="TeamRepository{TTeamEntity,TMember}.RemoveMemberFromAllTeamsAsync"/> strips the
/// user's member entries from every team containing them (regardless of membership state) and reports
/// the number of teams touched.
/// </summary>
public class TeamRepositoryRemoveFromAllTeamsTests
{
    [Fact]
    public async Task RemoveMemberFromAllTeamsAsync_RemovesFromEveryTeamAndReturnsCount()
    {
        var teams = new[]
        {
            Team("T1", Member("u-target"), Member("u-other")),
            Team("T2", Member("u-target"))
        };
        var collection = Substitute.For<ITeamRepositoryCollection<TestTeamEntity, TestMember>>();
        collection.GetAsync(Arg.Any<Expression<Func<TestTeamEntity, bool>>>()).Returns(teams.ToAsyncEnumerable());

        var sut = new TeamRepository<TestTeamEntity, TestMember>(collection);
        var count = await sut.RemoveMemberFromAllTeamsAsync("u-target");

        Assert.Equal(2, count);
        await collection.ReceivedWithAnyArgs(2)
            .UpdateOneAsync(default(FilterDefinition<TestTeamEntity>), default(UpdateDefinition<TestTeamEntity>));
    }

    [Fact]
    public async Task RemoveMemberFromAllTeamsAsync_UserInNoTeams_ReturnsZeroWithoutWrites()
    {
        var collection = Substitute.For<ITeamRepositoryCollection<TestTeamEntity, TestMember>>();
        collection.GetAsync(Arg.Any<Expression<Func<TestTeamEntity, bool>>>())
            .Returns(Array.Empty<TestTeamEntity>().ToAsyncEnumerable());

        var sut = new TeamRepository<TestTeamEntity, TestMember>(collection);
        var count = await sut.RemoveMemberFromAllTeamsAsync("u-none");

        Assert.Equal(0, count);
        await collection.DidNotReceiveWithAnyArgs()
            .UpdateOneAsync(default(FilterDefinition<TestTeamEntity>), default(UpdateDefinition<TestTeamEntity>));
    }

    private static TestTeamEntity Team(string key, params TestMember[] members)
        => new() { Key = key, Name = $"Team {key}", Members = members };

    private static TestMember Member(string userKey)
        => new() { Key = userKey, State = MembershipState.Member };

    public record TestTeamEntity : TeamEntityBase<TestMember>;

    public record TestMember : TeamMemberBase;
}

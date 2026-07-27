using Tharga.MongoDB.Disk;
using Tharga.MongoDB.Interception;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// The database-boundary guard. It exists for what the registration APIs cannot reach: code that gets to
/// the database without passing through the authorization layer at all — including a consumer's own
/// repositories, which Tharga.Team never sees.
/// </summary>
public class TeamAccessInterceptorTests
{
    private static CollectionCallInfo Call(InterceptionPoint point = InterceptionPoint.Invocation) => new()
    {
        CollectionName = "Team",
        Operation = "GetOneAsync",
        OperationType = Operation.Read,
        EntityType = typeof(object),
        Point = point
    };

    private static async Task<InterceptDecision> DecideAsync()
        => await new TeamAccessInterceptor().BeforeCallAsync(Call());

    [Fact]
    public async Task WithNoAuthorization_IsRejected()
    {
        var decision = await DecideAsync();

        Assert.True(decision.IsRejected);
        Assert.Contains("No authorization covers this call", decision.Reason);
    }

    [Fact]
    public async Task WithTeamAuthorization_Proceeds()
    {
        using var _ = TeamAccess.ForTeam("team-a");

        Assert.False((await DecideAsync()).IsRejected);
    }

    [Fact]
    public async Task WithSystemAuthorization_Proceeds()
    {
        using var _ = TeamAccess.System("nightly retention");

        Assert.False((await DecideAsync()).IsRejected);
    }

    [Fact]
    public async Task WithDeliberateUncheckedAccess_Proceeds()
    {
        using var _ = TeamAccess.Unchecked("startup seeding");

        Assert.False((await DecideAsync()).IsRejected);
    }

    [Fact]
    public async Task AfterTheScopeCloses_IsRejectedAgain()
    {
        using (TeamAccess.ForTeam("team-a"))
        {
            Assert.False((await DecideAsync()).IsRejected);
        }

        Assert.True((await DecideAsync()).IsRejected);
    }

    [Fact]
    public void RunsAtInvocationOnly()
    {
        // Deferred operations do their database work at enumeration, potentially after the authorizing
        // scope has gone. Checking there would reject legitimate calls.
        Assert.Equal(InterceptionPoint.Invocation, new TeamAccessInterceptor().Points);
    }

    // ---- The ambient scope itself ----

    [Fact]
    public void NestedScopes_RestoreThePreviousDecision()
    {
        using (TeamAccess.ForTeam("team-a"))
        {
            using (TeamAccess.ForTeam("team-b"))
            {
                Assert.Equal("team-b", TeamAccess.Current.TeamKey);
            }

            Assert.Equal("team-a", TeamAccess.Current.TeamKey);
        }

        Assert.Null(TeamAccess.Current);
    }

    [Fact]
    public async Task TheDecision_FlowsIntoAwaitedWork()
    {
        using var _ = TeamAccess.ForTeam("team-a");

        await Task.Yield();
        await Task.Run(() => Task.Delay(1));

        Assert.Equal("team-a", TeamAccess.Current?.TeamKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeliberateAccess_RequiresAReason(string reason)
    {
        Assert.Throws<ArgumentException>(() => TeamAccess.System(reason));
        Assert.Throws<ArgumentException>(() => TeamAccess.Unchecked(reason));
    }
}

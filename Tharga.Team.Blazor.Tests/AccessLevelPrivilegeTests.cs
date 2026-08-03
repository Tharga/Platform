using Tharga.Team;
using Tharga.Team.Blazor.Features.Simulation;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Comparing access levels by privilege, which is the opposite of comparing them by value.
/// </summary>
/// <remarks>
/// Its own type and its own tests because the obvious implementation is wrong in a way that compiles and
/// reads correctly: the enum runs <c>Owner=0, Administrator=1, User=2, Viewer=3</c>, so
/// <c>Math.Min(simulated, real)</c> — the instinctive guard against escalation — picks the <i>more</i>
/// privileged of the two.
/// </remarks>
public class AccessLevelPrivilegeTests
{
    [Theory]
    [InlineData(AccessLevel.Viewer, AccessLevel.Owner)]
    [InlineData(AccessLevel.User, AccessLevel.Owner)]
    [InlineData(AccessLevel.Administrator, AccessLevel.Owner)]
    [InlineData(AccessLevel.Viewer, AccessLevel.Administrator)]
    [InlineData(AccessLevel.User, AccessLevel.User)]
    public void ADeEscalationIsAllowed(AccessLevel candidate, AccessLevel actual)
    {
        Assert.True(AccessLevelPrivilege.IsNoMorePrivilegedThan(candidate, actual));
        Assert.Equal(candidate, AccessLevelPrivilege.Clamp(candidate, actual));
    }

    [Theory]
    [InlineData(AccessLevel.Owner, AccessLevel.Viewer)]
    [InlineData(AccessLevel.Owner, AccessLevel.Administrator)]
    [InlineData(AccessLevel.Administrator, AccessLevel.User)]
    [InlineData(AccessLevel.User, AccessLevel.Viewer)]
    public void AnEscalationIsRefusedAndFallsBackToTheRealLevel(AccessLevel candidate, AccessLevel actual)
    {
        Assert.False(AccessLevelPrivilege.IsNoMorePrivilegedThan(candidate, actual));
        Assert.Equal(actual, AccessLevelPrivilege.Clamp(candidate, actual));
    }

    /// <summary>
    /// <see cref="AccessLevel.Custom"/> sits at ordinal 4 but is not rank 4 — it grants no base scopes,
    /// which makes it the floor. Ordinal comparison gives the right answer here for the wrong reason, so
    /// it is asserted separately rather than folded into the ordering.
    /// </summary>
    [Theory]
    [InlineData(AccessLevel.Owner)]
    [InlineData(AccessLevel.Administrator)]
    [InlineData(AccessLevel.User)]
    [InlineData(AccessLevel.Viewer)]
    public void CustomIsTheFloor_ReachableFromAnyLevel(AccessLevel actual)
    {
        Assert.True(AccessLevelPrivilege.IsNoMorePrivilegedThan(AccessLevel.Custom, actual));
        Assert.Equal(AccessLevel.Custom, AccessLevelPrivilege.Clamp(AccessLevel.Custom, actual));
    }

    /// <summary>
    /// And nothing ranked is a de-escalation *from* the floor. Ordinal comparison would agree, but only
    /// by accident — <c>Viewer(3) &gt; Custom(4)</c> is false, which is right for the wrong reason.
    /// </summary>
    [Theory]
    [InlineData(AccessLevel.Owner)]
    [InlineData(AccessLevel.Administrator)]
    [InlineData(AccessLevel.User)]
    [InlineData(AccessLevel.Viewer)]
    public void NothingRankedIsADeEscalationFromCustom(AccessLevel candidate)
    {
        Assert.False(AccessLevelPrivilege.IsNoMorePrivilegedThan(candidate, AccessLevel.Custom));
        Assert.Equal(AccessLevel.Custom, AccessLevelPrivilege.Clamp(candidate, AccessLevel.Custom));
    }

    [Fact]
    public void CustomFromCustomIsUnchanged()
    {
        Assert.True(AccessLevelPrivilege.IsNoMorePrivilegedThan(AccessLevel.Custom, AccessLevel.Custom));
        Assert.Equal(AccessLevel.Custom, AccessLevelPrivilege.Clamp(AccessLevel.Custom, AccessLevel.Custom));
    }

    /// <summary>
    /// The property, over every pair: clamping never returns something more privileged than the real
    /// level. Exhaustive because the enum is small — five values, twenty-five pairs, no sampling.
    /// </summary>
    [Fact]
    public void ClampNeverReturnsSomethingMorePrivilegedThanTheRealLevel()
    {
        foreach (var actual in Enum.GetValues<AccessLevel>())
        {
            foreach (var candidate in Enum.GetValues<AccessLevel>())
            {
                var clamped = AccessLevelPrivilege.Clamp(candidate, actual);

                Assert.True(
                    AccessLevelPrivilege.IsNoMorePrivilegedThan(clamped, actual),
                    $"Clamp({candidate}, {actual}) returned {clamped}, which is more privileged than {actual}.");
            }
        }
    }

    /// <summary>
    /// The self-check on the exhaustive test above: it would pass vacuously if the enum were empty, and
    /// it silently stops covering a new level unless someone remembers. This fails when one is added, so
    /// the addition is a decision rather than an omission.
    /// </summary>
    [Fact]
    public void EveryAccessLevelIsAccountedFor()
    {
        Assert.Equal(
            [AccessLevel.Owner, AccessLevel.Administrator, AccessLevel.User, AccessLevel.Viewer, AccessLevel.Custom],
            Enum.GetValues<AccessLevel>());
    }
}

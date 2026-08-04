using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// When the team selector offers a search box.
/// </summary>
/// <remarks>
/// A short list is read faster than it is typed into, so a filter below the threshold is a control that
/// costs attention and saves none. The same judgement <c>AuditFilterVisibility</c> makes about the audit
/// filter bar, applied to a different control — and kept out of markup so it can be asserted directly
/// rather than through a render.
/// </remarks>
public class TeamSelectorFilterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public void BelowTheThreshold_ThereIsNoFilter(int teamCount)
    {
        Assert.False(TeamSelectorGate.ShowFilter(teamCount, TeamSelectorGate.DefaultFilterThreshold));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(200)]
    public void AtOrAboveTheThreshold_TheFilterAppears(int teamCount)
    {
        Assert.True(TeamSelectorGate.ShowFilter(teamCount, TeamSelectorGate.DefaultFilterThreshold));
    }

    /// <summary>The threshold is inclusive, so the documented number is the first that shows a filter.</summary>
    [Fact]
    public void TheThresholdItselfShowsTheFilter()
    {
        Assert.True(TeamSelectorGate.ShowFilter(3, threshold: 3));
        Assert.False(TeamSelectorGate.ShowFilter(2, threshold: 3));
    }

    /// <summary>A host lowering it — as the sample does, so the filter is reachable with few teams.</summary>
    [Fact]
    public void AHostCanLowerTheThreshold()
    {
        Assert.True(TeamSelectorGate.ShowFilter(2, threshold: 2));
        Assert.False(TeamSelectorGate.ShowFilter(2, TeamSelectorGate.DefaultFilterThreshold));
    }

    /// <summary>
    /// An explicit answer wins outright, in both directions. <c>true</c> and <c>false</c> each mean "I
    /// have decided"; the threshold is for everyone who has not.
    /// </summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    public void ForcingItOn_IgnoresTheThreshold(int teamCount, bool expected)
    {
        Assert.Equal(expected, TeamSelectorGate.ShowFilter(teamCount, TeamSelectorGate.DefaultFilterThreshold, allowFiltering: true));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void ForcingItOff_IgnoresTheThreshold(int teamCount)
    {
        Assert.False(TeamSelectorGate.ShowFilter(teamCount, TeamSelectorGate.DefaultFilterThreshold, allowFiltering: false));
    }

    /// <summary>
    /// The self-check: without it, "forcing off returns false" would be indistinguishable from the
    /// threshold happening to say no, and the parameter could be ignored entirely.
    /// </summary>
    [Fact]
    public void TheOverrideIsWhatDecides_NotTheThresholdAgreeing()
    {
        // 100 teams is far above the default, so only the override can make this false.
        Assert.True(TeamSelectorGate.ShowFilter(100, TeamSelectorGate.DefaultFilterThreshold));
        Assert.False(TeamSelectorGate.ShowFilter(100, TeamSelectorGate.DefaultFilterThreshold, allowFiltering: false));

        // One team is far below it, so only the override can make this true.
        Assert.False(TeamSelectorGate.ShowFilter(1, TeamSelectorGate.DefaultFilterThreshold));
        Assert.True(TeamSelectorGate.ShowFilter(1, TeamSelectorGate.DefaultFilterThreshold, allowFiltering: true));
    }

    /// <summary>
    /// The default is a judgement, not a measurement — pinned so changing it is a decision someone makes
    /// rather than a number that drifts.
    /// </summary>
    [Fact]
    public void TheDefaultThresholdIsEight()
    {
        Assert.Equal(8, TeamSelectorGate.DefaultFilterThreshold);
    }
}

using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Cards while the team list is short, a grid once it is not.
/// </summary>
/// <remarks>
/// Two shapes for two situations. Cards suit a handful — the expand affordance is obvious and a grid of
/// three rows looks like an administrative report of nothing much. Past the threshold that reverses:
/// cards cannot be sorted, filtered or paged, and a page of stacked accordions is not a list.
/// </remarks>
public class TeamListPresentationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void AShortListIsDrawnAsCards(int teamCount)
    {
        Assert.False(TeamListPresentation.ShowAsGrid(teamCount, TeamListPresentation.DefaultThreshold));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(40)]
    public void ALongListIsDrawnAsAGrid(int teamCount)
    {
        Assert.True(TeamListPresentation.ShowAsGrid(teamCount, TeamListPresentation.DefaultThreshold));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void AHostCanPinTheLayout(int teamCount)
    {
        Assert.False(TeamListPresentation.ShowAsGrid(teamCount, TeamListPresentation.DefaultThreshold, TeamListLayout.Cards));
        Assert.True(TeamListPresentation.ShowAsGrid(teamCount, TeamListPresentation.DefaultThreshold, TeamListLayout.Grid));
    }

    /// <summary>
    /// The self-check: pinning must be what decides, not the threshold happening to agree. Each case
    /// below is on the wrong side of the default, so only the pin can produce the expected answer.
    /// </summary>
    [Fact]
    public void PinningIsWhatDecides_NotTheThresholdAgreeing()
    {
        Assert.True(TeamListPresentation.ShowAsGrid(1000, TeamListPresentation.DefaultThreshold));
        Assert.False(TeamListPresentation.ShowAsGrid(1000, TeamListPresentation.DefaultThreshold, TeamListLayout.Cards));

        Assert.False(TeamListPresentation.ShowAsGrid(1, TeamListPresentation.DefaultThreshold));
        Assert.True(TeamListPresentation.ShowAsGrid(1, TeamListPresentation.DefaultThreshold, TeamListLayout.Grid));
    }

    [Fact]
    public void AutoIsTheDefaultAndDefersToTheThreshold()
    {
        Assert.Equal(TeamListLayout.Auto, default(TeamListLayout));
        Assert.Equal(
            TeamListPresentation.ShowAsGrid(9, TeamListPresentation.DefaultThreshold),
            TeamListPresentation.ShowAsGrid(9, TeamListPresentation.DefaultThreshold, TeamListLayout.Auto));
    }

    /// <summary>
    /// The list and the selector move together. They are two decisions — whether to draw a grid, and
    /// whether to offer a search box — but both turn on the same fact about the same collection, so a
    /// caller who sees one change should see the other change with it.
    /// </summary>
    [Fact]
    public void TheSelectorAndTheListShareOneThreshold()
    {
        Assert.Equal(TeamListPresentation.DefaultThreshold, TeamSelectorGate.DefaultFilterThreshold);

        for (var count = 0; count < 20; count++)
        {
            Assert.Equal(
                TeamListPresentation.ShowAsGrid(count, TeamListPresentation.DefaultThreshold),
                TeamSelectorGate.ShowFilter(count, TeamSelectorGate.DefaultFilterThreshold));
        }
    }

    /// <summary>Pinned so moving it is a decision someone makes, not a number that drifts.</summary>
    [Fact]
    public void TheDefaultThresholdIsEight()
    {
        Assert.Equal(8, TeamListPresentation.DefaultThreshold);
    }
}

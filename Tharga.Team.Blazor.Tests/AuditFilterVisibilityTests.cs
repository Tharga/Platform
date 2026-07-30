using Tharga.Team.Blazor.Features.Audit;

namespace Tharga.Team.Blazor.Tests;

public class AuditFilterVisibilityTests
{
    [Theory]
    [InlineData(2, false, true)]
    [InlineData(9, false, true)]
    public void SeveralOptionsAndNotPinned_IsShown(int optionCount, bool isPinned, bool expected)
    {
        Assert.Equal(expected, AuditFilterVisibility.ShouldShow(optionCount, isPinned));
    }

    /// <summary>
    /// The case that drove this: a system API key has no team and a team key has exactly one, so the Team
    /// filter offered a control that could not change the result.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FewerThanTwoOptions_IsHidden(int optionCount)
    {
        Assert.False(AuditFilterVisibility.ShouldShow(optionCount, false));
    }

    /// <summary>
    /// A pinned dimension is not the reader's to change, however many values happen to exist for it.
    /// </summary>
    [Fact]
    public void Pinned_IsHiddenEvenWithManyOptions()
    {
        Assert.False(AuditFilterVisibility.ShouldShow(50, true));
    }
}

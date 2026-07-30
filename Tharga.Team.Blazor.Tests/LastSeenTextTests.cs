using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// "Never" versus a blank cell. On a grid whose purpose is deciding whether an account or a team is still
/// in use, "we have no value" and "this never happened" are different answers.
/// </summary>
public class LastSeenTextTests
{
    [Fact]
    public void NoValue_ReadsAsNever()
    {
        Assert.True(LastSeenText.IsNever(null));
    }

    [Fact]
    public void AValue_DoesNotReadAsNever()
    {
        Assert.False(LastSeenText.IsNever(new DateTime(2026, 1, 1)));
    }

    /// <summary>
    /// Guards the boundary: default(DateTime) is a real value the store can return, and rendering it as
    /// "Never" would hide a genuine — if odd — timestamp.
    /// </summary>
    [Fact]
    public void DefaultDateTime_IsAValue_NotNever()
    {
        Assert.False(LastSeenText.IsNever(default(DateTime)));
    }
}

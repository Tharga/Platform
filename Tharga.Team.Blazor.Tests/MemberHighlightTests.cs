using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for <see cref="MemberHighlight"/> — the "is this row me?" decision behind the current-user
/// highlight in the team member grid.
/// </summary>
public class MemberHighlightTests
{
    [Fact]
    public void IsCurrentMember_MatchingKeys_IsTrue()
    {
        Assert.True(MemberHighlight.IsCurrentMember("user-1", "user-1"));
    }

    [Fact]
    public void IsCurrentMember_DifferentKeys_IsFalse()
    {
        Assert.False(MemberHighlight.IsCurrentMember("user-1", "user-2"));
    }

    [Theory]
    [InlineData(null, "user-1")]
    [InlineData("user-1", null)]
    [InlineData("", "user-1")]
    [InlineData("user-1", "")]
    public void IsCurrentMember_MissingKey_IsFalse(string memberKey, string userKey)
    {
        Assert.False(MemberHighlight.IsCurrentMember(memberKey, userKey));
    }

    /// <summary>Two invited members both carrying a null key must not both read as "you".</summary>
    [Fact]
    public void IsCurrentMember_BothNull_IsFalse()
    {
        Assert.False(MemberHighlight.IsCurrentMember(null, null));
    }

    [Fact]
    public void IsCurrentMember_IsCaseSensitive()
    {
        Assert.False(MemberHighlight.IsCurrentMember("User-1", "user-1"));
    }
}

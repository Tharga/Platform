using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The team surface stores per-team overrides only. Submitting the displayed default unchanged must
/// persist no override, so the row keeps tracking the user's own later renames instead of pinning a
/// copy of the name at the moment someone opened the editor.
/// </summary>
public class MemberNamePolicyTests
{
    [Fact]
    public void ResolveOverride_DistinctName_IsStored()
    {
        Assert.Equal("Dan", MemberNamePolicy.ResolveOverride("Dan", "Daniel Bohlin"));
    }

    [Fact]
    public void ResolveOverride_TrimsSurroundingWhitespace()
    {
        Assert.Equal("Dan", MemberNamePolicy.ResolveOverride("  Dan  ", "Daniel Bohlin"));
    }

    [Fact]
    public void ResolveOverride_MatchingDefault_StoresNoOverride()
    {
        Assert.Null(MemberNamePolicy.ResolveOverride("Daniel Bohlin", "Daniel Bohlin"));
    }

    [Fact]
    public void ResolveOverride_MatchingDefaultAfterTrim_StoresNoOverride()
    {
        Assert.Null(MemberNamePolicy.ResolveOverride("  Daniel Bohlin  ", "Daniel Bohlin"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveOverride_BlankInput_StoresNoOverride(string input)
    {
        Assert.Null(MemberNamePolicy.ResolveOverride(input, "Daniel Bohlin"));
    }

    [Fact]
    public void ResolveOverride_BlankInput_WithNoResolvableDefault_StoresNoOverride()
    {
        Assert.Null(MemberNamePolicy.ResolveOverride(null, null));
    }
}

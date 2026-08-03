using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Where a generated invitation link points.
/// </summary>
/// <remarks>
/// Tharga/Team#191: the path was hardcoded to <c>/team</c>, so a host that gated that page for its own
/// staff closed the one page that redeems an invite to precisely the people who needed it — silently
/// from every angle.
/// <para>
/// The string handling gets its own type and its own tests because a malformed path breaks the link the
/// same silent way: the base URI already ends in a slash, so a leading one produces a double slash, and
/// a host will write the route three different ways meaning the same thing.
/// </para>
/// </remarks>
public class InvitePathResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    public void WithNothingConfigured_LinksPointWhereTheyAlwaysDid(string configured)
    {
        Assert.Equal("team", InvitePathResolver.Resolve(configured));
        Assert.Equal(InvitePathResolver.DefaultPath, InvitePathResolver.Resolve(configured));
    }

    /// <summary>
    /// A blank value is a misconfiguration, not a request for the site root — a link to the root would
    /// look plausible and redeem nothing, which is the failure this whole issue is about.
    /// </summary>
    [Fact]
    public void AnEmptyPathIsNotTakenToMeanTheSiteRoot()
    {
        Assert.NotEqual(string.Empty, InvitePathResolver.Resolve(""));
    }

    /// <summary>
    /// Three ways of writing the same route. The base URI already ends in a slash, so a leading one would
    /// produce <c>https://host//invitation</c>.
    /// </summary>
    [Theory]
    [InlineData("invitation")]
    [InlineData("/invitation")]
    [InlineData("invitation/")]
    [InlineData("/invitation/")]
    [InlineData("  /invitation/  ")]
    public void AHostCanWriteTheRouteHoweverTheyLike(string configured)
    {
        Assert.Equal("invitation", InvitePathResolver.Resolve(configured));
    }

    /// <summary>A nested route keeps its inner slashes; only the ends are trimmed.</summary>
    [Fact]
    public void ANestedRouteIsPreserved()
    {
        Assert.Equal("account/invitation", InvitePathResolver.Resolve("/account/invitation/"));
    }

    /// <summary>
    /// The self-check: without it, every assertion above would still pass if Resolve simply returned the
    /// default for everything, and the option would be silently inert.
    /// </summary>
    [Fact]
    public void AConfiguredPathIsActuallyUsed()
    {
        Assert.NotEqual(InvitePathResolver.DefaultPath, InvitePathResolver.Resolve("invitation"));
        Assert.Equal("invitation", InvitePathResolver.Resolve("invitation"));
    }

    /// <summary>The option is opt-in: a host that never sets it sees no change.</summary>
    [Fact]
    public void TheOptionDefaultsToUnset()
    {
        Assert.Null(new ThargaBlazorOptions().InvitePath);
        Assert.Equal("team", InvitePathResolver.Resolve(new ThargaBlazorOptions().InvitePath));
    }
}

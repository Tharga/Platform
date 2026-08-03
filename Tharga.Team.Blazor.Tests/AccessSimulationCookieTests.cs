using Tharga.Team;
using Tharga.Team.Blazor.Features.Simulation;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The carrier: untrusted, unsigned, and never allowed to throw.
/// </summary>
/// <remarks>
/// Anything unparseable means "no simulation", which returns the caller to their real access. That is
/// the safe direction and the same outcome as clearing the cookie — whereas throwing would turn a
/// hand-edited value into a broken session.
/// </remarks>
public class AccessSimulationCookieTests
{
    private static AccessSimulation Sample() => new()
    {
        Kind = AccessSimulationKind.User,
        Label = "Bob",
        Scopes = ["orders:read", "orders:write"],
        AccessLevel = AccessLevel.Viewer,
        DropSystemScopes = true,
        DropAppRoles = true
    };

    /// <remarks>
    /// Compared property by property rather than with <c>Assert.Equal(record, record)</c>. The generated
    /// record equality compares <see cref="AccessSimulation.Scopes"/> by <i>reference</i>, so two
    /// simulations carrying identical scopes are unequal — a round trip would always "differ" and the
    /// assertion would say nothing about the payload it is meant to check.
    /// </remarks>
    [Fact]
    public void ASimulationSurvivesARoundTrip()
    {
        var original = Sample();

        var restored = AccessSimulationCookie.Read(AccessSimulationCookie.Write(original));

        Assert.NotNull(restored);
        Assert.Equal(original.Kind, restored.Kind);
        Assert.Equal(original.Label, restored.Label);
        Assert.Equal(original.Scopes, restored.Scopes);
        Assert.Equal(original.AccessLevel, restored.AccessLevel);
        Assert.Equal(original.DropSystemScopes, restored.DropSystemScopes);
        Assert.Equal(original.DropAppRoles, restored.DropAppRoles);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!!")]
    [InlineData("bm90IGpzb24=")]                      // valid base64, not JSON
    [InlineData("eyJraW5kIjo5OTk5fQ==")]              // JSON, unknown enum value
    [InlineData("eyJTY29wZXMiOiJub3QtYW4tYXJyYXkifQ==")] // JSON, wrong shape
    public void AnythingUnparseableMeansNoSimulation(string value)
    {
        Assert.Null(AccessSimulationCookie.Read(value));
    }

    [Fact]
    public void WritingNullClearsIt()
    {
        Assert.Equal(string.Empty, AccessSimulationCookie.Write(null));
        Assert.Null(AccessSimulationCookie.Read(AccessSimulationCookie.Write(null)));
    }

    /// <summary>
    /// A value naming scopes the caller does not hold parses fine — it is the filter, not the parser,
    /// that makes it harmless. Asserted here so the division of responsibility is not later "tidied"
    /// into validation that would give the parser a security role it cannot fulfil.
    /// </summary>
    [Fact]
    public void AForgedValueParsesAndIsHarmlessByTheFilterInstead()
    {
        var forged = AccessSimulationCookie.Write(new AccessSimulation
        {
            Kind = AccessSimulationKind.Scopes,
            Label = "forged",
            Scopes = ["firewall:open", "billing:manage"],
            AccessLevel = AccessLevel.Owner
        });

        var parsed = AccessSimulationCookie.Read(forged);

        Assert.NotNull(parsed);
        Assert.Contains("firewall:open", parsed.Scopes);
    }

    [Fact]
    public void AMissingScopeListReadsAsEmptyRatherThanNull()
    {
        var written = AccessSimulationCookie.Write(new AccessSimulation
        {
            Kind = AccessSimulationKind.Scopes, Label = "none", Scopes = null
        });

        Assert.Empty(AccessSimulationCookie.Read(written).Scopes);
    }
}

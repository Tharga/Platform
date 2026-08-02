using System.Reflection;
using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Mcp.Tests;

/// <summary>
/// REST and MCP agree about which team a request acts on, because they ask the same code.
/// </summary>
/// <remarks>
/// The audit work spent a whole feature establishing that surfaces each holding a copy of a rule will
/// drift. These tests hold the same line one level up — for <i>which team</i> rather than <i>may they
/// read it</i>.
/// <para>
/// <b>They live in this project deliberately.</b> Only here are both <c>Tharga.Team.Service</c> and
/// <c>Tharga.Team.Mcp</c> loaded, so a scan across the packages can actually see both. The first version
/// sat in the service tests, could not see the MCP assembly at all, and passed against the duplicate it
/// was written to catch.
/// </para>
/// </remarks>
public class TeamContextAgreementTests
{
    private static Type[] TeamTypes() =>
    [
        .. new[] { typeof(TeamContextOptions).Assembly, typeof(McpTeamOptions).Assembly }
            .Distinct()
            .SelectMany(SafeTypes)
    ];

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
    }

    /// <summary>
    /// The scan really does cover both packages. Asserted first, because a scan that examines one
    /// assembly and reports success is indistinguishable from one that found nothing wrong — and this
    /// exact guard already made that mistake once.
    /// </summary>
    [Fact]
    public void TheGuard_ScansBothPackages()
    {
        var assemblies = TeamTypes().Select(t => t.Assembly.GetName().Name).Distinct().ToArray();

        Assert.Contains("Tharga.Team.Service", assemblies);
        Assert.Contains("Tharga.Team.Mcp", assemblies);
    }

    /// <summary>
    /// The header name is configured in exactly one place.
    /// </summary>
    /// <remarks>
    /// It briefly was not: the MCP options carried their own <c>TeamKeyHeader</c> beside the core one,
    /// both defaulting to the same string. Nothing would have failed until a host configured one surface
    /// and not the other, at which point the same call would be named differently depending on the door.
    /// That is the shape <c>ConsentOptions</c> had to be rescued from.
    /// </remarks>
    [Fact]
    public void TheHeaderName_IsDeclaredOnce()
    {
        var declaring = TeamTypes()
            .Where(t => t.GetProperty("TeamKeyHeader", BindingFlags.Public | BindingFlags.Instance) != null)
            .Select(t => t.FullName)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal([typeof(TeamContextOptions).FullName], declaring);
    }

    /// <summary>
    /// A team key reaches no other team by <b>any</b> route now available to it: there is no parameter,
    /// and the header is refused. Stated as one test because the claim is about the absence of routes,
    /// and absence is what gets forgotten when a new one is added.
    /// </summary>
    [Fact]
    public async Task ATeamKey_HasNoRouteToAnotherTeam()
    {
        var teamService = Substitute.For<ITeamService>();
        teamService.GetTeamByKeyAsync(Arg.Any<string>())
            .Returns(new ConsentingTeam("team-2", ["Support"], AccessLevel.Administrator));

        var resolver = new TeamContextResolver(teamService, Substitute.For<IScopeRegistry>());

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(TeamClaimTypes.TeamKey, "team-1"), new Claim(TeamClaimTypes.ApiKeyId, "k1")], "Test"));

        // team-2 consents to anyone, and a bound credential still may not go there.
        var context = await resolver.ResolveAsync(principal, "team-2");

        Assert.True(context.IsRefused);
        Assert.Equal(TeamContextRefusal.Contradiction, context.Refusal);
    }

    private sealed record ConsentingTeam(string Key, string[] ConsentedRoles, AccessLevel? ConsentAccessLevel) : ITeam
    {
        public string Name => Key;
        public string Icon => null;
    }
}

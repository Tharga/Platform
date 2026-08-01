using System.Reflection;
using Tharga.Mcp;
using Tharga.Team;

namespace Tharga.Team.Mcp.Tests;

/// <summary>
/// No MCP resource or tool provider may inject <see cref="ITeamService"/>.
/// </summary>
/// <remarks>
/// The counterpart to the component guard in <c>Tharga.Team.Blazor.Tests</c>, kept here because this is
/// the project that can see the provider assembly. Same rule, same reason: <c>ITeamService</c> is the
/// host's storage contract and is deliberately unchecked, so a first-level surface injecting it bypasses
/// authorization.
/// <para>
/// MCP matters more than it looks. Its only automatic gate is the provider's <i>scope class</i> —
/// <c>McpScope.System</c> means a Developer role, <c>McpScope.Team</c> means membership — and neither
/// consults a scope. Routing providers through the gated services is what makes an API key's scopes
/// checked by the same code that checks a user's.
/// </para>
/// </remarks>
public class ProviderInjectionArchitectureTests
{
    private static readonly Assembly ProviderAssembly = typeof(TeamResourceProvider).Assembly;

    public static TheoryData<Type> Providers()
    {
        var data = new TheoryData<Type>();
        foreach (var type in ProviderAssembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false })
                     .Where(t => typeof(IMcpResourceProvider).IsAssignableFrom(t) || typeof(IMcpToolProvider).IsAssignableFrom(t))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void Provider_DoesNotInject_TheInternalTeamContract(Type provider)
    {
        var offending = provider.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Where(p => p.ParameterType == typeof(ITeamService))
            .Select(p => p.Name)
            .ToArray();

        Assert.True(offending.Length == 0,
            $"{provider.Name} injects {nameof(ITeamService)} via constructor parameter " +
            $"'{string.Join("', '", offending)}'. That contract is deliberately unchecked and bypasses " +
            $"authorization. Inject {nameof(ITeamManagementService)}, {nameof(ITeamDirectoryService)} or " +
            $"{nameof(ITeamOversightService)} instead.");
    }

    /// <summary>The guard must be looking at something — an empty set would pass silently.</summary>
    [Fact]
    public void TheGuard_FindsProvidersToCheck()
    {
        Assert.NotEmpty(Providers());
    }
}

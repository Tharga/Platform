using System.Reflection;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Guards the precondition the whole classification scheme rests on: a service interface must be wholly
/// team-bound or wholly system-wide, so its registration can declare the rule once and have it be true of
/// every method. A mixed interface is what forced authorization back into per-method annotations and let
/// `IApiKeyManagementService` apply one policy to two different kinds of operation.
/// </summary>
/// <remarks>
/// Runs over the types rather than over the registrations, so a method added to a scope-bearing interface
/// fails here even before anyone wires it up.
/// </remarks>
public class ServiceScopeArchitectureTests
{
    private const string TeamKeyParameterName = "teamKey";

    public static TheoryData<Type> ScopeBearingInterfaces()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(ITeamService).Assembly.GetTypes()
                     .Where(t => t.IsInterface && t.IsPublic)
                     .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<RequireScopeAttribute>() != null))
                     .OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ScopeBearingInterfaces))]
    public void ScopeBearingInterface_IsWhollyTeamBound_OrWhollySystemWide(Type serviceType)
    {
        var methods = serviceType.GetMethods();
        var teamBound = methods.Where(NamesATeam).Select(m => m.Name).ToArray();
        var systemWide = methods.Where(m => !NamesATeam(m)).Select(m => m.Name).ToArray();

        Assert.True(
            teamBound.Length == 0 || systemWide.Length == 0,
            $"'{serviceType.Name}' mixes team-bound and system-wide operations, so no single registration " +
            $"can be true of all of them. Team-bound: {string.Join(", ", teamBound)}. " +
            $"System-wide: {string.Join(", ", systemWide)}. Split the interface.");
    }

    [Fact]
    public void ScopeBearingInterfaces_AreDiscovered()
    {
        // Guards the theory above against silently covering nothing if discovery ever breaks.
        Assert.NotEmpty(ScopeBearingInterfaces());
    }

    private static bool NamesATeam(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length > 0
               && parameters[0].ParameterType == typeof(string)
               && parameters[0].Name == TeamKeyParameterName;
    }
}

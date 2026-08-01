using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// No component may inject <see cref="ITeamService"/>.
/// </summary>
/// <remarks>
/// <c>ITeamService</c> is the host's storage contract and is <b>deliberately unchecked</b> — framework
/// code reads through it while constructing the very claims that would authorize the read, so gating it
/// would be circular and break sign-in. A component injecting it therefore bypasses authorization
/// entirely.
/// <para>
/// That is not hypothetical: it is how <c>team:read</c> came to be registered, documented, granted — and
/// checked by nothing. Every read surface reached around the gate because reaching around it was the
/// path of least resistance and nothing said otherwise.
/// </para>
/// <para>
/// <b>Reflects over types, not over files.</b> The manual sweep that preceded this test undercounted the
/// surfaces three times — 8, then 11, then 13 — because each pass grepped a narrower slice than the one
/// before (<c>.razor</c> only, missing <c>.razor.cs</c> and plain <c>.cs</c> components). A guard built
/// the same way would inherit the same blind spot.
/// </para>
/// <para>
/// <see cref="TeamStateService"/> is not caught here and should not be: it is framework code, not a
/// component, and the rule bans components, controllers and MCP providers. It is what bridges
/// <c>SelectTeamEvent</c> so a component no longer needs the internal contract to hear it.
/// </para>
/// </remarks>
public class ComponentInjectionArchitectureTests
{
    private static readonly Assembly BlazorAssembly = typeof(TeamStateService).Assembly;

    public static TheoryData<Type> Components()
    {
        var data = new TheoryData<Type>();
        foreach (var type in BlazorAssembly.GetTypes()
                     .Where(t => typeof(ComponentBase).IsAssignableFrom(t) && !t.IsAbstract)
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Components))]
    public void Component_DoesNotInject_TheInternalTeamContract(Type component)
    {
        var offending = InternalContractDependencies(component).ToArray();

        Assert.True(offending.Length == 0,
            $"{component.Name} injects {nameof(ITeamService)} via {string.Join(", ", offending)}. " +
            $"That contract is deliberately unchecked and bypasses authorization. Inject " +
            $"{nameof(ITeamManagementService)} (one team), {nameof(ITeamDirectoryService)} (your own " +
            $"teams), {nameof(ITeamOversightService)} (every team) or {nameof(ITeamInvitationService)} " +
            $"(an invite code) instead.");
    }

    /// <summary>Sanity check on the guard itself: it must be looking at something.</summary>
    [Fact]
    public void TheGuard_FindsComponentsToCheck()
    {
        Assert.NotEmpty(Components());
    }

    /// <summary>
    /// The guard would pass trivially if it could not see an injection, so prove it can by pointing it at
    /// a type that has one. Without this, deleting the detection logic would leave every test green.
    /// </summary>
    [Fact]
    public void TheGuard_DetectsAnInjectionWhenThereIsOne()
    {
        Assert.NotEmpty(InternalContractDependencies(typeof(OffendingComponent)));
    }

    private sealed class OffendingComponent : ComponentBase
    {
        [Inject] public ITeamService TeamService { get; set; }
    }

    /// <summary>
    /// Property injection is how a <c>.razor</c> <c>@inject</c> compiles, and constructor injection is
    /// how a plain component takes a dependency. Both count.
    /// </summary>
    private static IEnumerable<string> InternalContractDependencies(Type component)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var property in component.GetProperties(flags)
                     .Where(p => p.PropertyType == typeof(ITeamService))
                     .Where(p => p.GetCustomAttribute<InjectAttribute>() != null))
        {
            yield return $"property '{property.Name}'";
        }

        foreach (var parameter in component.GetConstructors(flags)
                     .SelectMany(c => c.GetParameters())
                     .Where(p => p.ParameterType == typeof(ITeamService)))
        {
            yield return $"constructor parameter '{parameter.Name}'";
        }
    }
}

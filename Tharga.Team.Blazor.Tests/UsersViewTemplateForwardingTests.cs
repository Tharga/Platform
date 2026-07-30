using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// <see cref="UsersView{TMember}"/> is a wrapper over <c>UsersListView</c> and <c>TeamsListView</c>, so a
/// host that renders the packaged view can only extend a tab the wrapper forwards a template for. It
/// forwarded the Users-tab hooks but neither Teams-tab hook, leaving a host that wanted a team row action
/// to abandon the wrapper and re-implement the tab shell — including the directory-only tab the wrapper
/// itself provides. These tests pin both halves so the wrapper cannot silently fall behind its children
/// again.
/// </summary>
/// <remarks>
/// Reflection rather than rendering, matching <see cref="AdminViewSurfaceTests"/>: the project has no
/// bUnit. <b>This asserts the parameter surface, not the wiring</b> — removing the pass-through from the
/// markup while leaving the property declared would still pass. Replace these with rendering assertions
/// when bUnit lands; this is the first case that should move.
/// </remarks>
public class UsersViewTemplateForwardingTests
{
    private static Type Component(string mangledName)
        => typeof(UserViewModel).Assembly.GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.User" && t.Name == mangledName);

    private static PropertyInfo AssertParameter(Type type, string name, Type expected)
    {
        var prop = type.GetProperty(name);
        Assert.NotNull(prop);
        Assert.Equal(expected, prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
        return prop;
    }

    [Fact]
    public void UsersView_ForwardsTheTeamActionsTemplate()
    {
        AssertParameter(Component("UsersView`1"), "TeamActionsTemplate", typeof(RenderFragment<TeamViewModel>));
    }

    [Fact]
    public void UsersView_ForwardsTheMemberActionsTemplate()
    {
        AssertParameter(Component("UsersView`1"), "MemberActionsTemplate", typeof(RenderFragment<TeamMemberInfo>));
    }

    /// <summary>
    /// The wrapper's parameter must be the same type the child accepts, or the forward would not compile
    /// and a host would be writing its template against a different shape than the one that renders.
    /// </summary>
    [Theory]
    [InlineData("TeamActionsTemplate")]
    [InlineData("MemberActionsTemplate")]
    public void WrapperTemplateType_MatchesTheChild(string parameterName)
    {
        var wrapper = Component("UsersView`1").GetProperty(parameterName);
        var child = Component("TeamsListView`1").GetProperty(parameterName);

        Assert.NotNull(child);
        Assert.NotNull(wrapper);
        Assert.Equal(child.PropertyType, wrapper.PropertyType);
    }

    /// <summary>
    /// Guards the asymmetry this feature removes: every row-action hook a child exposes should be reachable
    /// from the wrapper. Fails if a future child gains a template the wrapper does not forward.
    /// </summary>
    [Fact]
    public void EveryChildActionTemplate_IsReachableFromTheWrapper()
    {
        var wrapperParameters = Component("UsersView`1")
            .GetProperties()
            .Where(x => x.GetCustomAttribute<ParameterAttribute>() != null)
            .Select(x => x.Name)
            .ToHashSet();

        var childTemplates = new[] { Component("UsersListView`1"), Component("TeamsListView`1") }
            .SelectMany(x => x.GetProperties())
            .Where(x => x.GetCustomAttribute<ParameterAttribute>() != null)
            .Where(x => x.Name.EndsWith("Template", StringComparison.Ordinal))
            .Select(x => x.Name)
            .Distinct()
            .ToArray();

        var notForwarded = childTemplates.Where(x => !wrapperParameters.Contains(x)).ToArray();

        Assert.NotEmpty(childTemplates);
        Assert.Equal([], notForwarded);
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the host-override of the built-in "Create team" action (Tharga/Platform#123):
/// the <see cref="ThargaBlazorOptions.CreateTeamPath"/> option, the <c>CreateTeamRequested</c>
/// callback parameter on <c>TeamSelector</c>/<c>TeamComponent</c>, and the precedence resolver.
/// Reflection-based to match the other component parameter tests (no bUnit in this project).
/// </summary>
public class CreateTeamOverrideTests
{
    [Fact]
    public void ThargaBlazorOptions_HasCreateTeamPath_String_DefaultsToNull()
    {
        var prop = typeof(ThargaBlazorOptions).GetProperty(nameof(ThargaBlazorOptions.CreateTeamPath));

        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop.PropertyType);
        Assert.Null(new ThargaBlazorOptions().CreateTeamPath);
    }

    [Fact]
    public void TeamComponent_HasCreateTeamRequested_EventCallback_Parameter()
    {
        var prop = ResolveComponent("TeamComponent`1").GetProperty("CreateTeamRequested");

        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void TeamSelector_HasCreateTeamRequested_EventCallback_Parameter()
    {
        var prop = ResolveComponent("TeamSelector").GetProperty("CreateTeamRequested");

        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Theory]
    // Callback always wins, regardless of a configured path.
    [InlineData(true, null, nameof(CreateTeamAction.Callback))]
    [InlineData(true, "/onboarding", nameof(CreateTeamAction.Callback))]
    // No callback: a non-empty path navigates.
    [InlineData(false, "/onboarding", nameof(CreateTeamAction.Navigate))]
    // No callback, no path: built-in behavior.
    [InlineData(false, null, nameof(CreateTeamAction.BuiltIn))]
    [InlineData(false, "", nameof(CreateTeamAction.BuiltIn))]
    [InlineData(false, "   ", nameof(CreateTeamAction.BuiltIn))]
    public void Resolve_AppliesCallbackThenPathThenBuiltIn(bool hasCallback, string createTeamPath, string expected)
    {
        Assert.Equal(expected, CreateTeamActionResolver.Resolve(hasCallback, createTeamPath).ToString());
    }

    private static Type ResolveComponent(string typeName)
    {
        var blazorAssembly = typeof(ThargaBlazorRegistration).Assembly;
        return blazorAssembly
            .GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.Team" && t.Name == typeName);
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Smoke tests for the <c>ApiKeyView.ShowScopeOverrides</c> parameter added under
/// <see href="https://github.com/Tharga/Platform/issues/71">Tharga/Platform#71</see>.
/// </summary>
public class ApiKeyViewScopeOverridesTests
{
    [Fact]
    public void ApiKeyView_HasShowScopeOverridesParameter_DefaultFalse()
    {
        var componentType = ResolveApiKeyView();
        var prop = componentType.GetProperty("ShowScopeOverrides");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());

        var instance = Activator.CreateInstance(componentType);
        Assert.False((bool)prop.GetValue(instance));
    }

    [Fact]
    public void ApiKeyView_StillHasShowAuditLogButtonParameter()
    {
        // Regression guard — adding ShowScopeOverrides must not have displaced the existing parameter.
        var componentType = ResolveApiKeyView();
        var prop = componentType.GetProperty("ShowAuditLogButton");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void ApiKeyView_HasShowRolesParameter_DefaultFalse()
    {
        var componentType = ResolveApiKeyView();
        var prop = componentType.GetProperty("ShowRoles");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());

        var instance = Activator.CreateInstance(componentType);
        Assert.False((bool)prop.GetValue(instance));
    }

    private static Type ResolveApiKeyView()
    {
        var blazorAssembly = typeof(Tharga.Team.Blazor.Framework.ThargaBlazorRegistration).Assembly;
        return blazorAssembly
            .GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.Api" && t.Name == "ApiKeyView");
    }
}

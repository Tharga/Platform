using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class TenantRoleVisibilityProviderTests
{
    [Fact]
    public async Task Default_Provider_Shows_Every_Role()
    {
        var provider = new AllRolesVisibleTenantRoleVisibilityProvider();

        Assert.True(await provider.IsRoleVisibleAsync("team-1", "CaseAdministrator"));
        Assert.True(await provider.IsRoleVisibleAsync("team-2", "AnyOtherRole"));
    }

    [Fact]
    public void AddThargaTenantRoles_Registers_Default_Visibility_Provider()
    {
        var services = new ServiceCollection();
        services.AddThargaTenantRoles(r => r.Register("Editor", "doc:edit"));

        var provider = services.BuildServiceProvider().GetService<ITenantRoleVisibilityProvider>();

        Assert.IsType<AllRolesVisibleTenantRoleVisibilityProvider>(provider);
    }

    [Fact]
    public void Consumer_Registered_Provider_Overrides_Default()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantRoleVisibilityProvider, FeatureGatedVisibilityProvider>();
        services.AddThargaTenantRoles(r => r.Register("Editor", "doc:edit"));

        var provider = services.BuildServiceProvider().GetService<ITenantRoleVisibilityProvider>();

        Assert.IsType<FeatureGatedVisibilityProvider>(provider);
    }

    private sealed class FeatureGatedVisibilityProvider : ITenantRoleVisibilityProvider
    {
        public Task<bool> IsRoleVisibleAsync(string teamKey, string roleName, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}

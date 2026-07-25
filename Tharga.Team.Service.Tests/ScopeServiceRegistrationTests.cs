using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// The registration APIs, exercised through a real container. A service is only protected if its
/// registration installs the enforcing wrapper — `[RequireScope]` on the interface does nothing on its
/// own, which is how <c>IApiKeyManagementService</c> came to be registered with a plain
/// <c>AddScoped</c> and enforce nothing at all.
/// </summary>
public class ScopeServiceRegistrationTests
{
    private sealed class TeamServiceImplementation : IScopedTestService
    {
        public string ReadMethod(string teamKey) => "read-ok";
        public string DownloadMethod(string teamKey) => "download-ok";
        public string DeleteMethod(string teamKey) => "delete-ok";
        public string UnprotectedMethod(string teamKey) => "unprotected-ok";
    }

    private sealed class SystemServiceImplementation : ISystemScopedTestService
    {
        public string ReadMethod() => "read-ok";
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> register, string teamKey, params string[] scopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var scope in scopes) claims.Add(new Claim(TeamClaimTypes.Scope, scope));

        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        register(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddTeamService_Rejects_A_Team_The_Caller_Holds_No_Scope_For()
    {
        using var provider = BuildProvider(
            s => s.AddTeamService<IScopedTestService, TeamServiceImplementation>(),
            "team-a", "doc:delete");

        var service = provider.GetRequiredService<IScopedTestService>();

        Assert.Throws<UnauthorizedAccessException>(() => service.DeleteMethod("team-b"));
    }

    [Fact]
    public void AddTeamService_Allows_The_Team_The_Caller_Holds_The_Scope_For()
    {
        using var provider = BuildProvider(
            s => s.AddTeamService<IScopedTestService, TeamServiceImplementation>(),
            "team-a", "doc:delete");

        var service = provider.GetRequiredService<IScopedTestService>();

        Assert.Equal("delete-ok", service.DeleteMethod("team-a"));
    }

    [Fact]
    public void AddTeamService_Resolves_A_Wrapper_Not_The_Implementation()
    {
        using var provider = BuildProvider(
            s => s.AddTeamService<IScopedTestService, TeamServiceImplementation>(),
            "team-a", "doc:delete");

        var service = provider.GetRequiredService<IScopedTestService>();

        Assert.IsNotType<TeamServiceImplementation>(service);
    }

    [Fact]
    public void AddSystemService_Succeeds_With_No_Team_Selected()
    {
        using var provider = BuildProvider(
            s => s.AddSystemService<ISystemScopedTestService, SystemServiceImplementation>(),
            teamKey: null, "system:read");

        var service = provider.GetRequiredService<ISystemScopedTestService>();

        Assert.Equal("read-ok", service.ReadMethod());
    }

    [Fact]
    public void AddSystemService_Without_The_Scope_Is_Rejected()
    {
        using var provider = BuildProvider(
            s => s.AddSystemService<ISystemScopedTestService, SystemServiceImplementation>(),
            teamKey: null);

        var service = provider.GetRequiredService<ISystemScopedTestService>();

        Assert.Throws<UnauthorizedAccessException>(() => service.ReadMethod());
    }

    [Fact]
    public void ApiKeyManagement_Is_Registered_Through_The_Guarding_Path()
    {
        var services = new ServiceCollection();
        services.AddThargaApiKeys();

        var descriptor = services.Last(d => d.ServiceType == typeof(IApiKeyManagementService));

        // A plain AddScoped<TService, TImplementation> records an ImplementationType and installs no
        // wrapper — the shape this service had while enforcing nothing.
        Assert.Null(descriptor.ImplementationType);
        Assert.NotNull(descriptor.ImplementationFactory);
    }

    [Fact]
    public void SystemApiKeyManagement_Is_Registered_Separately_From_The_Team_Service()
    {
        var services = new ServiceCollection();
        services.AddThargaApiKeys();

        Assert.Contains(services, d => d.ServiceType == typeof(ISystemApiKeyManagementService));
        Assert.Contains(services, d => d.ServiceType == typeof(IApiKeyManagementService));
    }
}

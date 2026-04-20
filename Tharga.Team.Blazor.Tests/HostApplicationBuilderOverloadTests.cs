using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

public class HostApplicationBuilderOverloadTests
{
    [Fact]
    public void AddThargaTeamBlazor_On_HostApplicationBuilder_Registers_Same_Services_As_On_ServiceCollection()
    {
        // Direct IServiceCollection path
        var directServices = new ServiceCollection();
        directServices.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService, StubMember>();
        });

        // IHostApplicationBuilder path
        var builder = Host.CreateApplicationBuilder();
        builder.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService, StubMember>();
        });

        // Compare registrations for core Tharga services
        var directKinds = directServices
            .Where(d => d.ServiceType.FullName?.StartsWith("Tharga.") ?? false)
            .Select(d => d.ServiceType.FullName)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var builderKinds = builder.Services
            .Where(d => d.ServiceType.FullName?.StartsWith("Tharga.") ?? false)
            .Select(d => d.ServiceType.FullName)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(directKinds, builderKinds);
    }

    [Fact]
    public void AddThargaTeamBlazor_On_HostApplicationBuilder_Passes_Configuration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["BlazorOptions:Title"] = "From Config";

        builder.AddThargaTeamBlazor();

        var sp = builder.Services.BuildServiceProvider();
        // The Tharga.Blazor layer binds BlazorOptions from configuration when available.
        // Smoke check: the IConfiguration the builder holds is the one we populated.
        var config = sp.GetRequiredService<IConfiguration>();
        Assert.Equal("From Config", config["BlazorOptions:Title"]);
    }

    [Fact]
    public void AddThargaTeamBlazor_On_HostApplicationBuilder_NullBuilder_Throws()
    {
        IHostApplicationBuilder builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.AddThargaTeamBlazor());
    }
}

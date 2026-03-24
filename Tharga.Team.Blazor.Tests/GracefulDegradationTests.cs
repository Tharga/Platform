using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tharga.Team;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Tests;

public class GracefulDegradationTests
{
    [Fact]
    public void CompositeAuditLogger_IsNull_WhenNotRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var logger = provider.GetService<CompositeAuditLogger>();

        Assert.Null(logger);
    }

    [Fact]
    public void CompositeAuditLogger_IsResolved_WhenRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThargaAuditLogging();
        var provider = services.BuildServiceProvider();

        var logger = provider.GetService<CompositeAuditLogger>();

        Assert.NotNull(logger);
    }

    [Fact]
    public void ApiKeyManagementService_IsNull_WhenNotRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var service = provider.GetService<IApiKeyManagementService>();

        Assert.Null(service);
    }
}

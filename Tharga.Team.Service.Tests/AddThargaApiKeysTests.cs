using Microsoft.Extensions.DependencyInjection;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests;

public class AddThargaApiKeysTests
{
    [Fact]
    public void Registers_IApiKeyRepository()
    {
        var services = new ServiceCollection();
        services.AddThargaApiKeys();

        Assert.Contains(services, d => d.ServiceType == typeof(IApiKeyRepository));
    }

    [Fact]
    public void Registers_IApiKeyRepositoryCollection()
    {
        var services = new ServiceCollection();
        services.AddThargaApiKeys();

        Assert.Contains(services, d => d.ServiceType == typeof(IApiKeyRepositoryCollection));
    }

    [Fact]
    public void Registers_IApiKeyManagementService()
    {
        var services = new ServiceCollection();
        services.AddThargaApiKeys();

        Assert.Contains(services, d => d.ServiceType == typeof(IApiKeyManagementService));
    }
}

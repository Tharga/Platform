using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tharga.Team.Service.Tests;

public class AddThargaControllersOpenApiTests
{
    private const string DefaultDocumentName = "v1";

    [Fact]
    public void ConfigureOpenApi_accumulates_callbacks_and_runs_them_in_registration_order()
    {
        var options = new ThargaControllerOptions();
        var order = new List<int>();

        options.ConfigureOpenApi(_ => order.Add(1));
        options.ConfigureOpenApi(_ => order.Add(2));

        options.OpenApiConfigure!.Invoke(new OpenApiOptions());

        Assert.Equal([1, 2], order);
    }

    [Fact]
    public void ConfigureOpenApi_returns_the_same_instance_for_chaining()
    {
        var options = new ThargaControllerOptions();

        var result = options.ConfigureOpenApi(_ => { });

        Assert.Same(options, result);
    }

    [Fact]
    public void ConfigureOpenApi_null_callback_throws()
    {
        var options = new ThargaControllerOptions();

        Assert.Throws<ArgumentNullException>(() => options.ConfigureOpenApi(null!));
    }

    [Fact]
    public void OpenApiConfigure_is_null_when_no_hook_is_registered()
    {
        var options = new ThargaControllerOptions();

        Assert.Null(options.OpenApiConfigure);
    }

    [Fact]
    public void AddThargaControllers_invokes_the_consumer_hook_against_the_managed_document_options()
    {
        var invoked = false;
        OpenApiOptions captured = null;

        var services = new ServiceCollection();
        services.AddThargaControllers(o => o.ConfigureOpenApi(api =>
        {
            invoked = true;
            captured = api;
        }));

        using var provider = services.BuildServiceProvider();
        var materialized = provider.GetRequiredService<IOptionsMonitor<OpenApiOptions>>().Get(DefaultDocumentName);

        Assert.True(invoked);
        Assert.Same(materialized, captured);
    }

    [Fact]
    public void AddThargaControllers_without_a_hook_still_materializes_the_managed_document_options()
    {
        var services = new ServiceCollection();
        services.AddThargaControllers();

        using var provider = services.BuildServiceProvider();

        var materialized = provider.GetRequiredService<IOptionsMonitor<OpenApiOptions>>().Get(DefaultDocumentName);

        Assert.NotNull(materialized);
    }
}

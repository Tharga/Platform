using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class ApiKeyLifecycleRegistrationTests
{
    private static IApiKey SampleKey()
    {
        var k = Substitute.For<IApiKey>();
        k.Key.Returns("key-1");
        k.ApiKey.Returns("raw-token");
        k.TeamKey.Returns("team-1");
        k.Name.Returns("My Key");
        k.Tags.Returns([]);
        return k;
    }

    private static IApiKeyAdministrationService InnerReturning(IApiKey key)
    {
        var inner = Substitute.For<IApiKeyAdministrationService>();
        inner.CreateKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AccessLevel>(), Arg.Any<string[]>(),
            Arg.Any<string[]>(), Arg.Any<DateTime?>(), Arg.Any<IReadOnlyList<Tag>>(), Arg.Any<string>())
            .Returns(key);
        return inner;
    }

    [Fact]
    public async Task Decorates_AdminService_And_Invokes_Handler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApiKeyAdministrationService>(_ => InnerReturning(SampleKey()));
        services.AddThargaApiKeyLifecycleHandler<RecordingA>();

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IApiKeyAdministrationService>();

        Assert.IsType<ApiKeyLifecycleDecorator>(admin);

        await admin.CreateKeyAsync("team-1", "My Key", AccessLevel.User);

        var handler = (RecordingBase)scope.ServiceProvider.GetServices<IApiKeyLifecycleHandler>().Single();
        var ctx = Assert.Single(handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Created, ctx.Reason);
        Assert.Equal("raw-token", ctx.PrivateToken);
    }

    [Fact]
    public async Task Multiple_Handlers_Decorate_Once_And_All_Fire()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApiKeyAdministrationService>(_ => InnerReturning(SampleKey()));
        services.AddThargaApiKeyLifecycleHandler<RecordingA>();
        services.AddThargaApiKeyLifecycleHandler<RecordingB>();

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IApiKeyAdministrationService>();

        await admin.CreateKeyAsync("team-1", "My Key", AccessLevel.User);

        var handlers = scope.ServiceProvider.GetServices<IApiKeyLifecycleHandler>().Cast<RecordingBase>().ToList();
        Assert.Equal(2, handlers.Count);
        // Single call each proves the decoration was applied once (a double-wrap would fire twice).
        Assert.All(handlers, h => Assert.Single(h.Calls));
    }

    [Fact]
    public void Throws_When_AdminService_Not_Registered()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddThargaApiKeyLifecycleHandler<RecordingA>());
    }

    private abstract class RecordingBase : IApiKeyLifecycleHandler
    {
        public readonly List<ApiKeyLifecycleContext> Calls = [];
        public Task OnApiKeyLifecycleAsync(ApiKeyLifecycleContext context)
        {
            Calls.Add(context);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingA : RecordingBase;
    private sealed class RecordingB : RecordingBase;
}

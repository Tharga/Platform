using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Reports, at startup, any persistence extension point the host's user service has left un-overridden.
/// </summary>
/// <remarks>
/// Each such member accepts a write, reports success and discards it. The failure is invisible: no
/// error, no log, and a feature that looks configured and is not. Saying so once at startup is the
/// cheapest place to find out.
/// <para>
/// <b>Logs an error rather than throwing, by default.</b> A throw is louder, and it would stop an
/// application that has been running for months from booting after a routine upgrade — over a feature it
/// may never use. The gap is pre-existing in every such case, so turning it into an outage is a worse
/// trade than making it impossible to miss in the log. A host that prefers the strict reading sets
/// <c>ThrowOnIncompleteUserService</c>.
/// </para>
/// </remarks>
internal sealed class UserServiceCompletenessCheck(
    IServiceProvider serviceProvider,
    Type userServiceType,
    bool throwOnIncomplete,
    ILogger<UserServiceCompletenessCheck> logger = null) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var gaps = UserServiceCompleteness.Find(
            userServiceType,
            iconStoreRegistered: sp.GetService<IIconStore>() != null,
            directoryRegistered: sp.GetService<IUserDirectoryService>() != null);

        if (gaps.Count == 0) return Task.CompletedTask;

        var detail = string.Join(Environment.NewLine, gaps.Select(g => $"  - {g}"));
        var message =
            $"'{userServiceType.Name}' does not override {gaps.Count} persistence extension point(s). " +
            $"Each accepts a write, reports success and discards it:{Environment.NewLine}{detail}{Environment.NewLine}" +
            "Override them, or derive from a storage base such as UserServiceRepositoryBase which implements all of them.";

        if (throwOnIncomplete) throw new InvalidOperationException(message);

        logger?.LogError("{Message}", message);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

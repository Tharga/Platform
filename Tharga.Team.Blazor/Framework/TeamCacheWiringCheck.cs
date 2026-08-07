using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Team;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Fails startup when a host has registered a custom <see cref="ITeamCache"/> that its own services are not
/// actually using.
/// </summary>
/// <remarks>
/// <b>This one throws, unlike <see cref="UserServiceCompletenessCheck"/>, and the difference is deliberate.</b>
/// That check reports pre-existing gaps a host may never have noticed, so turning a routine upgrade into an
/// outage would be the worse trade. This one can only fire when a host has <i>deliberately registered</i> a
/// custom cache — so firing means they configured something that is not happening, and the thing not happening
/// is authorization freshness across instances. There is nothing to weigh: it cannot fire for a host that has
/// not opted in, and for one that has, booting with the wrong cache is worse than not booting.
/// </remarks>
internal sealed class TeamCacheWiringCheck(
    IServiceProvider serviceProvider,
    Type teamServiceType,
    Type userServiceType) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        // The concrete types, not ITeamService/IUserService: those resolve to the decorator chain, and a
        // decorator holds no cache of its own.
        var unwired = TeamCacheWiring.FindUnwired(
            sp.GetService<ITeamCache>(),
            Resolve(sp, teamServiceType),
            Resolve(sp, userServiceType));

        if (unwired.Count > 0) throw new InvalidOperationException(TeamCacheWiring.DescribeFailure(unwired));

        return Task.CompletedTask;
    }

    /// <remarks>
    /// A service that cannot be constructed is not this check's business — the container's own validation and
    /// the completeness checks report that far better than a cache diagnostic would.
    /// </remarks>
    private static object Resolve(IServiceProvider sp, Type serviceType)
    {
        if (serviceType == null) return null;

        try
        {
            return sp.GetService(serviceType);
        }
        catch
        {
            return null;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

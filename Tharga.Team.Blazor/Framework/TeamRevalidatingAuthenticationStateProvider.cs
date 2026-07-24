using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Server auth-state provider that periodically revalidates the caller's team claims for the life of a
/// Blazor Server circuit (#127). On each interval it re-evaluates team membership, access level, tenant
/// scopes, and consent access; if they changed it refreshes the principal <b>in place</b> — the caller is
/// not signed out, their team access is simply brought up to date (including downgrades and removal). This
/// keeps both the UI and service-layer authorization (which reads the auth state when no HttpContext
/// exists) from acting on frozen claims.
/// </summary>
internal sealed class TeamRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;

    public TeamRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<ThargaBlazorOptions> options)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _interval = options.Value.ClaimRevalidation.Interval;
    }

    protected override TimeSpan RevalidationInterval => _interval;

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Resolve the team services from a fresh scope rather than the circuit's own: this background
        // loop must not share the circuit's scoped services with an in-flight render (thread-safety), and
        // it breaks the DI cycle in which the auth-state provider would otherwise depend on ITeamService,
        // which depends (via BlazorTeamPrincipalAccessor) back on the auth-state provider.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var revalidator = scope.ServiceProvider.GetRequiredService<TeamClaimRevalidator>();

        var refreshed = await revalidator.TryRefreshAsync(authenticationState.User);
        if (refreshed != null)
        {
            // Push the refreshed claims and keep the user signed in. Setting a new state cancels this
            // revalidation loop and starts a fresh one bound to the updated principal.
            SetAuthenticationState(Task.FromResult(new AuthenticationState(refreshed)));
        }

        // Team-claim changes never force a sign-out — the app session is still valid, only team access moved.
        return true;
    }
}

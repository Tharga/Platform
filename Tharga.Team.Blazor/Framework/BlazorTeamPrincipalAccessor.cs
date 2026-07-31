using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// <see cref="ITeamPrincipalAccessor"/> for Blazor apps that also expose an HTTP/API surface. Uses the
/// current <c>HttpContext</c> when one exists (controllers, SSR), falls back to
/// <see cref="AuthenticationStateProvider"/> in an interactive circuit (where <c>HttpContext</c> is null),
/// and yields no principal when it is in neither — so a single <c>[RequireScope]</c>/
/// <c>[RequireAccessLevel]</c> enforces every surface.
/// </summary>
/// <remarks>
/// The third case is the one that bites. Code with no request and no circuit — an MCP request handler, a
/// hosted service, a message handler — used to reach the circuit fallback, which calls an API valid only
/// inside a Razor component's DI scope, and every such call threw. That made every MCP resource read fail.
/// <para>
/// Returning no principal is the honest answer there: authorization refuses, which is what should happen
/// for a caller nothing can identify. Failing closed by crashing was never the intent.
/// </para>
/// </remarks>
public class BlazorTeamPrincipalAccessor(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider) : ITeamPrincipalAccessor
{
    /// <summary>
    /// Marker in the framework's message for "you are not in a Razor component's DI scope".
    /// </summary>
    /// <remarks>
    /// Matched on the message because the framework offers nothing else to ask: an unseeded
    /// <c>ServerAuthenticationStateProvider</c> is indistinguishable from a seeded one until it is called,
    /// and there is no public "am I in a circuit" signal. Narrow on purpose — any other
    /// <see cref="InvalidOperationException"/> propagates, so a circuit that is genuinely broken still
    /// surfaces instead of being reported as an anonymous caller.
    /// </remarks>
    private const string OutsideRazorScopeMarker = "outside of the DI scope";

    public async ValueTask<ClaimsPrincipal> GetCurrentAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
            return httpContext.User;

        try
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            return state.User;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(OutsideRazorScopeMarker, StringComparison.Ordinal))
        {
            // Neither an HTTP request nor a circuit: nothing can name this caller.
            return null;
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tharga.Team;

/// <summary>
/// Reads the caller from an <see cref="AuthenticationStateProvider"/> without assuming there is a circuit
/// to read from.
/// </summary>
/// <remarks>
/// <see cref="AuthenticationStateProvider"/> is a Razor-component-scoped service. Called outside that
/// scope — an MCP request handler, a hosted service, a message handler — the server implementation throws
/// rather than reporting "no caller", so any code that fell back to it as *the* way to identify a caller
/// crashed instead of treating the caller as anonymous.
/// <para>
/// One helper rather than one fix per call site: the same assumption appeared in
/// <c>UserServiceBase.GetClaims</c> and in the Blazor principal accessor, and fixing them separately would
/// leave the next one to be discovered the same way — from a stack trace.
/// </para>
/// </remarks>
public static class CircuitPrincipal
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

    /// <summary>
    /// The caller from <paramref name="authenticationStateProvider"/>, or <c>null</c> when there is no
    /// circuit to ask. Any failure other than "not in a circuit" propagates.
    /// </summary>
    public static async Task<ClaimsPrincipal> GetUserOrNullAsync(AuthenticationStateProvider authenticationStateProvider)
    {
        if (authenticationStateProvider == null) return null;

        try
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            return state.User;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(OutsideRazorScopeMarker, StringComparison.Ordinal))
        {
            return null;
        }
    }
}

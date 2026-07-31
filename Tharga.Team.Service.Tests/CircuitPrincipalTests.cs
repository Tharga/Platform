using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Reading the caller from a circuit provider when there may be no circuit.
/// </summary>
/// <remarks>
/// The same assumption — <c>AuthenticationStateProvider</c> is always callable — appeared in two places:
/// <c>UserServiceBase.GetClaims</c> and the Blazor principal accessor. Only the first was on the path that
/// broke MCP reads; fixing them separately would have left the next one to be found from a stack trace.
/// </remarks>
public class CircuitPrincipalTests
{
    /// <summary>
    /// The reported failure. An unseeded provider is what a non-circuit scope sees, and it throws rather
    /// than reporting "no caller".
    /// </summary>
    [Fact]
    public async Task OutsideACircuit_YieldsNoCaller()
    {
        var user = await CircuitPrincipal.GetUserOrNullAsync(new ServerAuthenticationStateProvider());

        Assert.True(user is null || user.Identity?.IsAuthenticated != true,
            "nothing outside a circuit can name a caller, so authorization must refuse rather than permit");
    }

    [Fact]
    public async Task InACircuit_YieldsTheCaller()
    {
        var expected = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "circuit")], "Test"));

        var user = await CircuitPrincipal.GetUserOrNullAsync(new SeededProvider(expected));

        Assert.Same(expected, user);
    }

    [Fact]
    public async Task NoProvider_YieldsNoCaller()
    {
        Assert.Null(await CircuitPrincipal.GetUserOrNullAsync(null));
    }

    /// <summary>
    /// The guard against this becoming a silent swallow: a provider failing for a real reason must still
    /// surface, or a broken circuit is indistinguishable from an anonymous caller.
    /// </summary>
    [Fact]
    public async Task AProviderFailingForARealReason_StillThrows()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await CircuitPrincipal.GetUserOrNullAsync(new BrokenProvider()));

        Assert.Contains("database", ex.Message);
    }

    private sealed class SeededProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    /// <summary>Derives from the framework's circuit provider, so a type check alone would not save us.</summary>
    private sealed class BrokenProvider : ServerAuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => throw new InvalidOperationException("The database is unreachable.");
    }
}

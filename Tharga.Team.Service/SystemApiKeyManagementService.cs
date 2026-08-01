using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Delegates system API key operations to <see cref="IApiKeyAdministrationService"/>, stamping the
/// creating user's identity from their claims rather than trusting a caller-supplied value.
/// </summary>
/// <remarks>
/// Split out of <c>ApiKeyManagementService</c>: these operations belong to no team, so they carry no
/// per-team owner-scoping and are registered as a system service. Keeping them alongside the team
/// operations is what previously let one authorization policy be applied to both.
/// </remarks>
public class SystemApiKeyManagementService : ISystemApiKeyManagementService
{
    private readonly IApiKeyAdministrationService _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SystemApiKeyManagementService(IApiKeyAdministrationService inner, IHttpContextAccessor httpContextAccessor = null)
    {
        _inner = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    public IAsyncEnumerable<IApiKey> GetSystemKeysAsync() => _inner.GetSystemKeysAsync();

    public Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null)
        => _inner.CreateSystemKeyAsync(name, scopes, expiryDate, GetCurrentUserIdentity());

    public Task<IApiKey> RefreshSystemKeyAsync(string key) => _inner.RefreshSystemKeyAsync(key);

    public Task LockSystemKeyAsync(string key) => _inner.LockSystemKeyAsync(key);

    public Task SetSystemKeyDisabledAsync(string key, bool disabled)
        => _inner.SetSystemKeyDisabledAsync(key, disabled, GetCurrentUserIdentity());

    public Task DeleteSystemKeyAsync(string key) => _inner.DeleteSystemKeyAsync(key);

    private string GetCurrentUserIdentity()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.Name)?.Value
               ?? user?.FindFirst("preferred_username")?.Value
               ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user?.FindFirst("name")?.Value;
    }
}

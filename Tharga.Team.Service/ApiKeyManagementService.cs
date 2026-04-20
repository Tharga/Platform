using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Delegates to <see cref="IApiKeyAdministrationService"/> for all operations.
/// Scope enforcement is handled by <see cref="ScopeProxy{T}"/>.
/// </summary>
public class ApiKeyManagementService : IApiKeyManagementService
{
    private readonly IApiKeyAdministrationService _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiKeyManagementService(IApiKeyAdministrationService inner, IHttpContextAccessor httpContextAccessor = null)
    {
        _inner = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    public IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey) => _inner.GetKeysAsync(teamKey);
    public Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, DateTime? expiryDate = null) => _inner.CreateKeyAsync(teamKey, name, accessLevel, roles, expiryDate);
    public Task<IApiKey> RefreshKeyAsync(string teamKey, string key) => _inner.RefreshKeyAsync(teamKey, key);
    public Task LockKeyAsync(string teamKey, string key) => _inner.LockKeyAsync(teamKey, key);
    public Task DeleteKeyAsync(string teamKey, string key) => _inner.DeleteKeyAsync(teamKey, key);

    public IAsyncEnumerable<IApiKey> GetSystemKeysAsync() => _inner.GetSystemKeysAsync();
    public Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null)
        => _inner.CreateSystemKeyAsync(name, scopes, expiryDate, GetCurrentUserIdentity());
    public Task<IApiKey> RefreshSystemKeyAsync(string key) => _inner.RefreshSystemKeyAsync(key);
    public Task LockSystemKeyAsync(string key) => _inner.LockSystemKeyAsync(key);
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

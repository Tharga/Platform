namespace Tharga.Team;

/// <summary>
/// User-facing service for API key management. All methods require the apikey:manage scope.
/// </summary>
public interface IApiKeyManagementService
{
    [RequireScope(ApiKeyScopes.Manage)]
    IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey);

    [RequireScope(ApiKeyScopes.Manage)]
    Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, DateTime? expiryDate = null);

    [RequireScope(ApiKeyScopes.Manage)]
    Task<IApiKey> RefreshKeyAsync(string teamKey, string key);

    [RequireScope(ApiKeyScopes.Manage)]
    Task LockKeyAsync(string teamKey, string key);

    [RequireScope(ApiKeyScopes.Manage)]
    Task DeleteKeyAsync(string teamKey, string key);

    [RequireScope(ApiKeyScopes.SystemManage)]
    IAsyncEnumerable<IApiKey> GetSystemKeysAsync();

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null);

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task<IApiKey> RefreshSystemKeyAsync(string key);

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task LockSystemKeyAsync(string key);

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task DeleteSystemKeyAsync(string key);
}

namespace Tharga.Team;

/// <summary>
/// User-facing service for API key management. All methods require the apikey:manage scope.
/// </summary>
public interface IApiKeyManagementService
{
    /// <summary>
    /// Returns the team's API keys for the caller. Team-wide keys are always included; owner-scoped
    /// ("private") keys are included per <paramref name="privateKeys"/>, intersected with the caller's
    /// entitlement (owner sees own; Developer-role sees all; <paramref name="allowPrivileged"/> additionally
    /// lets Administrator/Owner see private keys). Defaults preserve the original team-wide-only behaviour.
    /// </summary>
    [RequireScope(ApiKeyScopes.Manage)]
    IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey, PrivateKeyScope privateKeys = PrivateKeyScope.None, bool allowPrivileged = false);

    /// <summary>Creates a new API key. When <paramref name="ownerScoped"/> is true the key is private to the caller (owner = the caller's team-member key); a caller can only ever mint a key owned by themselves.</summary>
    [RequireScope(ApiKeyScopes.Manage)]
    Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, string[] scopeOverrides = null, DateTime? expiryDate = null, IReadOnlyList<Tag> tags = null, bool ownerScoped = false);

    [RequireScope(ApiKeyScopes.Manage)]
    Task<IApiKey> RefreshKeyAsync(string teamKey, string key);

    [RequireScope(ApiKeyScopes.Manage)]
    Task LockKeyAsync(string teamKey, string key);

    [RequireScope(ApiKeyScopes.Manage)]
    Task DeleteKeyAsync(string teamKey, string key);

    [RequireScope(ApiKeyScopes.Manage)]
    Task SetScopeOverridesAsync(string teamKey, string key, string[] scopes);

    [RequireScope(ApiKeyScopes.Manage)]
    Task SetRolesAsync(string teamKey, string key, string[] roles);

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

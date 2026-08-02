namespace Tharga.Team;

/// <summary>
/// User-facing management of **system** API keys — keys not bound to any team. All methods require the
/// <see cref="ApiKeyScopes.SystemManage"/> scope and no team need be selected.
/// </summary>
/// <remarks>
/// Separate from <see cref="IApiKeyManagementService"/> so each interface is wholly one kind: every
/// operation there names the team it acts on, every operation here acts on none. That homogeneity is what
/// lets the registration declare the rule once — <c>AddSystemService</c> here, <c>AddTeamService</c> there
/// — instead of leaving it to a per-method annotation somebody has to remember on the next method added.
/// </remarks>
public interface ISystemApiKeyManagementService
{
    [RequireScope(ApiKeyScopes.SystemManage)]
    IAsyncEnumerable<IApiKey> GetSystemKeysAsync();

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null);

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task<IApiKey> RefreshSystemKeyAsync(string key);

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task LockSystemKeyAsync(string key);

    /// <inheritdoc cref="IApiKeyManagementService.SetKeyDisabledAsync"/>
    [RequireScope(ApiKeyScopes.SystemManage)]
    Task SetSystemKeyDisabledAsync(string key, bool disabled);

    [RequireScope(ApiKeyScopes.SystemManage)]
    Task DeleteSystemKeyAsync(string key);
}

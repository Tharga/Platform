namespace Tharga.Team;

/// <summary>
/// Service for managing and validating API keys.
/// </summary>
public interface IApiKeyAdministrationService
{
    /// <summary>Looks up an API key by its raw value. Returns <c>null</c> if no match is found.</summary>
    Task<IApiKey> GetByApiKeyAsync(string apiKey);

    /// <summary>Returns all API keys for the specified team, creating default keys if fewer than AutoKeyCount exist.</summary>
    IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey);

    /// <summary>Creates a new API key with the specified settings (advanced mode).</summary>
    Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, DateTime? expiryDate = null);

    /// <summary>Generates a new API key value for an existing key entry. Returns the entity with the raw key visible once.</summary>
    Task<IApiKey> RefreshKeyAsync(string teamKey, string key);

    /// <summary>Locks an API key so it can no longer be used for authentication. Verifies team ownership.</summary>
    Task LockKeyAsync(string teamKey, string key);

    /// <summary>Deletes an API key. Verifies team ownership.</summary>
    Task DeleteKeyAsync(string teamKey, string key);

    /// <summary>Returns all system-level API keys (not bound to a team).</summary>
    IAsyncEnumerable<IApiKey> GetSystemKeysAsync();

    /// <summary>Creates a new system-level API key with the specified explicit scope set.</summary>
    /// <param name="name">Human-readable name for the key.</param>
    /// <param name="scopes">Explicit scopes granted to this key. Not resolved through AccessLevel/roles.</param>
    /// <param name="expiryDate">Optional expiry date.</param>
    /// <param name="createdBy">Identity of the user creating the key (for audit).</param>
    Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null, string createdBy = null);

    /// <summary>Regenerates a system key's raw value. Returns the entity with the raw key visible once.</summary>
    Task<IApiKey> RefreshSystemKeyAsync(string key);

    /// <summary>Locks a system API key so it can no longer authenticate.</summary>
    Task LockSystemKeyAsync(string key);

    /// <summary>Deletes a system API key.</summary>
    Task DeleteSystemKeyAsync(string key);
}

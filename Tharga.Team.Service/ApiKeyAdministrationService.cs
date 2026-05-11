using MongoDB.Bson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Team;
using Tharga.Toolkit;
using Tharga.Toolkit.Password;

namespace Tharga.Team.Service;

/// <summary>
/// Default implementation of <see cref="IApiKeyAdministrationService"/> using MongoDB storage.
/// </summary>
public class ApiKeyAdministrationService : IApiKeyAdministrationService
{
    private readonly IApiKeyRepository _repository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyAdministrationService> _logger;
    private readonly Tharga.Team.ApiKeyOptions _options;

    /// <summary>
    /// Creates a new instance using the specified repository and key hashing service.
    /// </summary>
    public ApiKeyAdministrationService(IApiKeyRepository repository, IApiKeyService apiKeyService, IOptions<Tharga.Team.ApiKeyOptions> options = null, ILogger<ApiKeyAdministrationService> logger = null)
    {
        _repository = repository;
        _apiKeyService = apiKeyService;
        _logger = logger;
        _options = options?.Value ?? new Tharga.Team.ApiKeyOptions();
    }

    /// <inheritdoc />
    public async Task<IApiKey> GetByApiKeyAsync(string apiKey)
    {
        var prefix = GetPrefix(apiKey);
        ApiKeyEntity item = null;

        if (prefix != null)
        {
            var candidates = await _repository.GetByPrefixAsync(prefix).ToArrayAsync();
            item = candidates.PickOneOrDefault(x => _apiKeyService.Verify(apiKey, x.ApiKeyHash), _logger, $"ApiKey verify prefix={prefix}");
        }

        if (item == null)
        {
            var allItems = await _repository.GetAsync().ToArrayAsync();
            item = allItems.PickOneOrDefault(x => _apiKeyService.Verify(apiKey, x.ApiKeyHash), _logger, "ApiKey verify full-scan");
        }

        if (item?.ExpiryDate != null && item.ExpiryDate < DateTime.UtcNow)
            return null;

        return item;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey)
    {
        await _repository.PurgeExpiredAsync();

        var count = 0;
        await foreach (var item in _repository.GetAsync())
        {
            if (item.TeamKey != teamKey) continue;
            count++;
            yield return item;
        }

        if (_options.AdvancedMode) yield break;

        for (var i = count; i < _options.AutoKeyCount; i++)
        {
            var name = IntegerExtensions.GetNameForNumber(i + 1);
            var expiryDate = GetDefaultExpiryDate();
            var entity = BuildKey(teamKey, name, [], AccessLevel.User, null, expiryDate);
            var created = await _repository.AddAsync(entity);

            if (_options.AutoLockKeys)
            {
                await _repository.LockKeyAsync(created.Key);
                created = created with { ApiKey = null };
            }

            yield return created;
        }
    }

    /// <inheritdoc />
    public async Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, DateTime? expiryDate = null)
    {
        expiryDate ??= GetDefaultExpiryDate();

        if (_options.MaxExpiryDays.HasValue && expiryDate.HasValue)
        {
            var maxDate = DateTime.UtcNow.AddDays(_options.MaxExpiryDays.Value);
            if (expiryDate > maxDate)
                throw new InvalidOperationException($"Expiry date cannot exceed {_options.MaxExpiryDays} days from now.");
        }

        var entity = BuildKey(teamKey, name, new Dictionary<string, string>(), accessLevel, roles, expiryDate);
        var created = await _repository.AddAsync(entity);

        if (_options.AutoLockKeys)
            await _repository.LockKeyAsync(created.Key);

        return created;
    }

    /// <inheritdoc />
    public async Task<IApiKey> RefreshKeyAsync(string teamKey, string key)
    {
        var item = await _repository.GetAsync(key);
        VerifyTeamOwnership(item, teamKey);
        var refreshed = BuildKey(teamKey, item.Name, item.Tags, item.AccessLevel ?? AccessLevel.Administrator, item.Roles, item.ExpiryDate);
        await _repository.UpdateAsync(key, refreshed);

        if (_options.AutoLockKeys)
            await _repository.LockKeyAsync(key);

        return refreshed;
    }

    /// <inheritdoc />
    public async Task LockKeyAsync(string teamKey, string key)
    {
        var item = await _repository.GetAsync(key);
        VerifyTeamOwnership(item, teamKey);
        await _repository.LockKeyAsync(key);
    }

    /// <inheritdoc />
    public async Task DeleteKeyAsync(string teamKey, string key)
    {
        var item = await _repository.GetAsync(key);
        VerifyTeamOwnership(item, teamKey);
        await _repository.DeleteAsync(key);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IApiKey> GetSystemKeysAsync()
    {
        await _repository.PurgeExpiredAsync();

        await foreach (var item in _repository.GetAsync())
        {
            if (item.TeamKey != null) continue;
            yield return item;
        }
    }

    /// <inheritdoc />
    public async Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null, string createdBy = null)
    {
        expiryDate ??= GetDefaultExpiryDate();

        if (_options.MaxExpiryDays.HasValue && expiryDate.HasValue)
        {
            var maxDate = DateTime.UtcNow.AddDays(_options.MaxExpiryDays.Value);
            if (expiryDate > maxDate)
                throw new InvalidOperationException($"Expiry date cannot exceed {_options.MaxExpiryDays} days from now.");
        }

        var entity = BuildSystemKey(name, scopes ?? Array.Empty<string>(), expiryDate, createdBy);
        var created = await _repository.AddAsync(entity);

        if (_options.AutoLockKeys)
            await _repository.LockKeyAsync(created.Key);

        return created;
    }

    /// <inheritdoc />
    public async Task<IApiKey> RefreshSystemKeyAsync(string key)
    {
        var item = await _repository.GetAsync(key);
        VerifySystemKey(item);
        var refreshed = BuildSystemKey(item.Name, item.SystemScopes ?? Array.Empty<string>(), item.ExpiryDate, item.CreatedBy);
        await _repository.UpdateAsync(key, refreshed);

        if (_options.AutoLockKeys)
            await _repository.LockKeyAsync(key);

        return refreshed;
    }

    /// <inheritdoc />
    public async Task LockSystemKeyAsync(string key)
    {
        var item = await _repository.GetAsync(key);
        VerifySystemKey(item);
        await _repository.LockKeyAsync(key);
    }

    /// <inheritdoc />
    public async Task DeleteSystemKeyAsync(string key)
    {
        var item = await _repository.GetAsync(key);
        VerifySystemKey(item);
        await _repository.DeleteAsync(key);
    }

    private static void VerifyTeamOwnership(ApiKeyEntity item, string teamKey)
    {
        if (item.TeamKey == null)
            throw new UnauthorizedAccessException("This is a system key; use the system-key methods.");
        if (item.TeamKey != teamKey)
            throw new UnauthorizedAccessException($"API key does not belong to team '{teamKey}'.");
    }

    private static void VerifySystemKey(ApiKeyEntity item)
    {
        if (item.TeamKey != null)
            throw new UnauthorizedAccessException("This is a team key; use the team-scoped methods.");
    }

    private DateTime? GetDefaultExpiryDate()
    {
        return _options.MaxExpiryDays.HasValue
            ? DateTime.UtcNow.AddDays(_options.MaxExpiryDays.Value)
            : null;
    }

    private ApiKeyEntity BuildKey(string teamKey, string name, Dictionary<string, string> tags, AccessLevel accessLevel, string[] roles, DateTime? expiryDate)
    {
        var apiKey = _apiKeyService.BuildApiKey(teamKey, () => StringExtension.GetRandomString(24, 32));
        var encryptedApiKey = _apiKeyService.Encrypt(apiKey);
        return new ApiKeyEntity
        {
            Id = ObjectId.GenerateNewId(),
            Key = Guid.NewGuid().ToString(),
            Name = name,
            ApiKey = apiKey,
            ApiKeyPrefix = GetPrefix(apiKey),
            TeamKey = teamKey,
            Tags = tags,
            ApiKeyHash = encryptedApiKey,
            AccessLevel = accessLevel,
            Roles = roles,
            ExpiryDate = expiryDate,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private ApiKeyEntity BuildSystemKey(string name, string[] scopes, DateTime? expiryDate, string createdBy)
    {
        var apiKey = _apiKeyService.BuildApiKey("system", () => StringExtension.GetRandomString(24, 32));
        var encryptedApiKey = _apiKeyService.Encrypt(apiKey);
        return new ApiKeyEntity
        {
            Id = ObjectId.GenerateNewId(),
            Key = Guid.NewGuid().ToString(),
            Name = name,
            ApiKey = apiKey,
            ApiKeyPrefix = GetPrefix(apiKey),
            TeamKey = null,
            Tags = new Dictionary<string, string>(),
            ApiKeyHash = encryptedApiKey,
            SystemScopes = scopes,
            ExpiryDate = expiryDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }

    private static string GetPrefix(string apiKey)
    {
        return apiKey?.Length >= 8 ? apiKey[..8] : null;
    }
}

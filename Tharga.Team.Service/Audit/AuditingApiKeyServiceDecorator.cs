using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Decorator that wraps <see cref="IApiKeyAdministrationService"/> and logs audit entries
/// for all mutation operations via <see cref="CompositeAuditLogger"/>.
/// Read operations are passed through without logging.
/// </summary>
public class AuditingApiKeyServiceDecorator : IApiKeyAdministrationService
{
    private readonly IApiKeyAdministrationService _inner;
    private readonly CompositeAuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string Feature = "apikey";

    public AuditingApiKeyServiceDecorator(IApiKeyAdministrationService inner, CompositeAuditLogger auditLogger, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
    }

    // Read operations — pass through

    public Task<IApiKey> GetByApiKeyAsync(string apiKey) => _inner.GetByApiKeyAsync(apiKey);
    public IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey) => _inner.GetKeysAsync(teamKey);

    // Mutation operations — log audit entries

    public async Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, DateTime? expiryDate = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.CreateKeyAsync(teamKey, name, accessLevel, roles, expiryDate);
            sw.Stop();
            Log("create", nameof(CreateKeyAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("create", nameof(CreateKeyAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task<IApiKey> RefreshKeyAsync(string teamKey, string key)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.RefreshKeyAsync(teamKey, key);
            sw.Stop();
            Log("refresh", nameof(RefreshKeyAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("refresh", nameof(RefreshKeyAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task LockKeyAsync(string teamKey, string key)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.LockKeyAsync(teamKey, key);
            sw.Stop();
            Log("lock", nameof(LockKeyAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("lock", nameof(LockKeyAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task DeleteKeyAsync(string teamKey, string key)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.DeleteKeyAsync(teamKey, key);
            sw.Stop();
            Log("delete", nameof(DeleteKeyAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("delete", nameof(DeleteKeyAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    private void Log(string action, string methodName, long durationMs, bool success, string errorMessage = null, string teamKey = null)
    {
        var entry = AuditHelper.BuildEntry(_httpContextAccessor, Feature, action, methodName, durationMs, success, errorMessage, teamKey);
        _auditLogger.Log(entry);
    }
}

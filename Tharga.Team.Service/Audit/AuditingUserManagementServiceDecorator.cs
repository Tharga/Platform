using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Decorator that wraps <see cref="IUserManagementService"/> and logs audit entries for user
/// administration: per-user verification (with outcome), bulk verification (one summary entry), and
/// deletion (with team count and directory result). The directory-only listing is a read with no side
/// effect and is not audited, consistent with team enumeration.
/// </summary>
public class AuditingUserManagementServiceDecorator : IUserManagementService
{
    private readonly IUserManagementService _inner;
    private readonly CompositeAuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string Feature = "user";

    public AuditingUserManagementServiceDecorator(IUserManagementService inner, CompositeAuditLogger auditLogger, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DirectoryVerificationResult> VerifyUserAsync(string userKey, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.VerifyUserAsync(userKey, cancellationToken);
            sw.Stop();
            Log("verify", nameof(VerifyUserAsync), sw.ElapsedMilliseconds, true, metadata: Meta(
                (AuditMetadataKeys.UserKey, userKey),
                (AuditMetadataKeys.DirectoryStatus, result?.Status.ToString())));
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("verify", nameof(VerifyUserAsync), sw.ElapsedMilliseconds, false, ex.Message,
                metadata: Meta((AuditMetadataKeys.UserKey, userKey)));
            throw;
        }
    }

    public async IAsyncEnumerable<UserVerificationResult> VerifyAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var count = 0;
        var success = false;
        try
        {
            await foreach (var result in _inner.VerifyAllAsync(cancellationToken))
            {
                count++;
                yield return result;
            }

            success = true;
        }
        finally
        {
            sw.Stop();
            Log("verify-all", nameof(VerifyAllAsync), sw.ElapsedMilliseconds, success,
                metadata: Meta((AuditMetadataKeys.VerifiedCount, count.ToString())));
        }
    }

    public async Task<UserDeleteResult> DeleteUserAsync(string userKey, bool deleteFromDirectory = false, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.DeleteUserAsync(userKey, deleteFromDirectory, cancellationToken);
            sw.Stop();
            Log("delete", nameof(DeleteUserAsync), sw.ElapsedMilliseconds, true, metadata: Meta(
                (AuditMetadataKeys.UserKey, userKey),
                (AuditMetadataKeys.MemberTeamCount, result?.RemovedTeamCount.ToString()),
                (AuditMetadataKeys.DirectoryDeleted, deleteFromDirectory ? result?.DirectoryDeleted.ToString() : null),
                (AuditMetadataKeys.DirectoryError, result?.DirectoryError)));
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("delete", nameof(DeleteUserAsync), sw.ElapsedMilliseconds, false, ex.Message,
                metadata: Meta((AuditMetadataKeys.UserKey, userKey)));
            throw;
        }
    }

    public IAsyncEnumerable<DirectoryUser> GetDirectoryOnlyUsersAsync(CancellationToken cancellationToken = default)
        => _inner.GetDirectoryOnlyUsersAsync(cancellationToken);

    private static Dictionary<string, string> Meta(params (string Key, string Value)[] pairs)
    {
        var metadata = new Dictionary<string, string>();
        foreach (var (key, value) in pairs)
        {
            if (value != null) metadata[key] = value;
        }
        return metadata;
    }

    private void Log(string action, string methodName, long durationMs, bool success, string errorMessage = null, IReadOnlyDictionary<string, string> metadata = null)
    {
        var entry = AuditHelper.BuildEntry(_httpContextAccessor, Feature, action, methodName, durationMs, success, errorMessage, teamKey: null, metadata);
        _auditLogger.Log(entry);
    }
}

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Interface for audit logging. Implementations handle storage (MongoDB, ILogger, etc.).
/// </summary>
public interface IAuditLogger
{
    void Log(AuditEntry entry);
    Task<AuditQueryResult> QueryAsync(AuditQuery query);
}

/// <summary>
/// Query parameters for retrieving audit entries.
/// Supports both single-value and multi-value filters.
/// </summary>
public record AuditQuery
{
    // Single-value filters (kept for backwards compat and simple queries)
    public string TeamKey { get; init; }
    public string CallerIdentity { get; init; }

    /// <summary>Filters entries by the API key Guid string that authenticated the caller (matches <see cref="AuditEntry.CallerKeyId"/>).</summary>
    public string CallerKeyId { get; init; }

    public string MethodName { get; init; }
    public string Feature { get; init; }
    public string Action { get; init; }
    public AuditCallerSource? CallerSource { get; init; }
    public AuditCallerType? CallerType { get; init; }
    public AuditEventType? EventType { get; init; }
    public bool? Success { get; init; }

    // Multi-value filters (take precedence over single-value when set)
    public string[] TeamKeys { get; init; }
    public string[] Features { get; init; }
    public string[] Actions { get; init; }
    public string[] Scopes { get; init; }
    public AuditEventType[] EventTypes { get; init; }

    // Paging and sorting
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 100;
    public string SortField { get; init; }
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Result of an audit query with items and total count for paging.
/// </summary>
public record AuditQueryResult
{
    public IReadOnlyList<AuditEntry> Items { get; init; } = Array.Empty<AuditEntry>();
    public int TotalCount { get; init; }
}

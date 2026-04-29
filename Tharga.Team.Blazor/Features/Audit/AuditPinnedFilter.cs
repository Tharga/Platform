using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Features.Audit;

/// <summary>
/// Locks one or more <see cref="AuditLogView"/> filter dimensions to a fixed value.
/// Pinned dimensions are visible in the UI but disabled, and they are forced into the
/// underlying <see cref="AuditQuery"/> regardless of any in-component filter state.
/// Use to scope the audit log to a single API key, team, caller, etc.
/// </summary>
public sealed record AuditPinnedFilter
{
    /// <summary>Pin to a specific API key Guid string.</summary>
    public string CallerKeyId { get; init; }

    /// <summary>Pin to a specific caller type (e.g. <see cref="AuditCallerType.ApiKey"/>).</summary>
    public AuditCallerType? CallerType { get; init; }

    /// <summary>Pin to a specific team key.</summary>
    public string TeamKey { get; init; }

    /// <summary>Pin to a specific caller identity (substring match, case-insensitive).</summary>
    public string CallerIdentity { get; init; }

    /// <summary>Pin to a single feature.</summary>
    public string Feature { get; init; }

    /// <summary>Pin to a single action.</summary>
    public string Action { get; init; }
}

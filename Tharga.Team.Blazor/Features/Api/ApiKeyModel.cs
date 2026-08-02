using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Api;

public record ApiKeyModel
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string ApiKey { get; init; }
    public string VisibleKey { get; set; }
    public AccessLevel AccessLevel { get; init; }
    public string[] Roles { get; init; }
    public string[] ScopeOverrides { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string CreatedBy { get; init; }
    public string OwnerMemberKey { get; init; }
    public bool IsPrivate => !string.IsNullOrEmpty(OwnerMemberKey);
    public DateTime? LastUsedAt { get; init; }
    public IReadOnlyList<Tag> Tags { get; init; } = Array.Empty<Tag>();
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate < DateTime.UtcNow;
    public DateTime? DisabledAt { get; init; }
    public string DisabledBy { get; init; }

    /// <summary>
    /// Disabled is a decision somebody made; expired is a date passing. They are shown differently on
    /// purpose — an operator who reads one as the other will either chase a key nobody turned off, or
    /// assume a contained key merely lapsed.
    /// </summary>
    public bool IsDisabled => DisabledAt.HasValue;
}

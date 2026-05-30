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
    public DateTime? LastUsedAt { get; init; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate < DateTime.UtcNow;
}

namespace Tharga.Team;

/// <summary>
/// Information handed to an <see cref="IApiKeyLifecycleHandler"/> when an API key is created,
/// recycled, or deleted. On <see cref="ApiKeyLifecycleReason.Created"/> and
/// <see cref="ApiKeyLifecycleReason.Recycled"/> the <see cref="PrivateToken"/> carries the raw key
/// value — the only moment it is available programmatically. The host is responsible for protecting
/// anything it captures; Tharga Team never persists, logs, or exposes the token elsewhere.
/// </summary>
/// <param name="Reason">Why the handler is being invoked.</param>
/// <param name="ApiKeyId">Stable public identifier of the key (<see cref="IApiKey.Key"/>). Use this to correlate a host-side copy.</param>
/// <param name="PrivateToken">The raw API key value. Non-null on <see cref="ApiKeyLifecycleReason.Created"/>/<see cref="ApiKeyLifecycleReason.Recycled"/>; null on <see cref="ApiKeyLifecycleReason.Deleted"/>.</param>
/// <param name="TeamKey">Owning team, or null for system keys.</param>
/// <param name="IsSystemKey">True for system (team-less) keys.</param>
/// <param name="Name">Human-readable key name. Null on <see cref="ApiKeyLifecycleReason.Deleted"/>.</param>
/// <param name="Tags">System-set tags on the key. Empty on <see cref="ApiKeyLifecycleReason.Deleted"/>.</param>
public record ApiKeyLifecycleContext(
    ApiKeyLifecycleReason Reason,
    string ApiKeyId,
    string PrivateToken,
    string TeamKey,
    bool IsSystemKey,
    string Name,
    IReadOnlyList<Tag> Tags);

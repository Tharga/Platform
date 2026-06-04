namespace Tharga.Team;

/// <summary>
/// Why an <see cref="IApiKeyLifecycleHandler"/> is being invoked.
/// </summary>
public enum ApiKeyLifecycleReason
{
    /// <summary>A new key was created. The private token is available.</summary>
    Created,

    /// <summary>An existing key's secret was regenerated (recycled). The new private token is available.</summary>
    Recycled,

    /// <summary>A key was deleted. No token is provided; use it to purge any host-side copy.</summary>
    Deleted,
}

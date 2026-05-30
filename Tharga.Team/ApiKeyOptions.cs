namespace Tharga.Team;

/// <summary>
/// Configuration options for API key management.
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// When true, enables full CRUD with custom names, access levels, roles, and expiry.
    /// When false, keys are auto-created and only refresh/lock are available.
    /// Default: false.
    /// </summary>
    public bool AdvancedMode { get; set; }

    /// <summary>
    /// Number of keys to auto-create per team in simple mode. Default: 2.
    /// </summary>
    public int AutoKeyCount { get; set; } = 2;

    /// <summary>
    /// When true, newly created keys are automatically locked after creation
    /// so the raw key value is only visible once. Default: false.
    /// </summary>
    public bool AutoLockKeys { get; set; }

    /// <summary>
    /// Maximum allowed expiry in days for API keys in advanced mode.
    /// Null means no maximum. Default: null.
    /// </summary>
    public int? MaxExpiryDays { get; set; }

    /// <summary>
    /// Minimum time between "last used" timestamp writes for a given key. A key authenticating more
    /// often than this only gets its <see cref="IApiKey.LastUsedAt"/> persisted once per window, to
    /// avoid a database write on every authenticated request. Set to <see cref="TimeSpan.Zero"/> to
    /// stamp on every successful authentication. Default: 1 minute.
    /// </summary>
    public TimeSpan LastUsedThrottle { get; set; } = TimeSpan.FromMinutes(1);
}

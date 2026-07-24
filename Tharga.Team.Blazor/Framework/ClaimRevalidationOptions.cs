namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Options for periodic team-claim revalidation on live Blazor Server circuits. See
/// <see cref="ThargaBlazorOptions.ClaimRevalidation"/>.
/// </summary>
public sealed class ClaimRevalidationOptions
{
    /// <summary>
    /// Whether team claims are revalidated periodically for the life of a Blazor Server circuit.
    /// Default <c>true</c>. Set to <c>false</c> to disable — claims are then computed only at circuit
    /// establishment and on a full reload / team switch (the pre-3.5 behaviour).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often the caller's team claims are re-evaluated. A change is applied in place without signing
    /// the user out; the caller's team access is therefore stale for at most this interval. A shorter
    /// interval narrows the window at the cost of one lightweight membership read per active circuit per
    /// interval. Default 30 minutes.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);
}

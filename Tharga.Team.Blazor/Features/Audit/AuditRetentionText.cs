namespace Tharga.Team.Blazor.Features.Audit;

/// <summary>
/// How the audit log describes its own retention to the reader.
/// </summary>
/// <remarks>
/// Pure and static so it is unit-testable — this project has no bUnit. The nil case matters: null, zero
/// and negative all mean "no TTL index" to <c>AuditOptions.RetentionDays</c>, so all three must produce
/// the same sentence rather than "kept for 0 days".
/// </remarks>
internal static class AuditRetentionText
{
    public static string Describe(int? retentionDays)
        => retentionDays is > 0
            ? $"Entries are kept for {retentionDays} days, then deleted automatically."
            : "Entries are kept indefinitely.";
}

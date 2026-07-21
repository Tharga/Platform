using System.Text.Json;
using Tharga.Team.Blazor.Features.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the CSV export encoding of audit metadata (#129). Arbitrary key/values are serialized to a
/// single JSON column so the CSV stays rectangular and round-trips.
/// </summary>
public class AuditMetadataExportTests
{
    [Fact]
    public void FormatMetadata_Null_IsEmpty()
    {
        Assert.Equal("", AuditLogView.FormatMetadata(null));
    }

    [Fact]
    public void FormatMetadata_Empty_IsEmpty()
    {
        Assert.Equal("", AuditLogView.FormatMetadata(new Dictionary<string, string>()));
    }

    [Fact]
    public void FormatMetadata_ProducesValidJsonWithEveryPair()
    {
        var json = AuditLogView.FormatMetadata(new Dictionary<string, string>
        {
            ["team.name.old"] = "Acme Ltd",
            ["team.name.new"] = "Acme Group"
        });

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Assert.Equal("Acme Ltd", parsed["team.name.old"]);
        Assert.Equal("Acme Group", parsed["team.name.new"]);
    }

    [Fact]
    public void FormatMetadata_IsKeyOrdered_ForStableDiffs()
    {
        var json = AuditLogView.FormatMetadata(new Dictionary<string, string>
        {
            ["zeta"] = "1",
            ["alpha"] = "2"
        });

        Assert.True(json.IndexOf("alpha", StringComparison.Ordinal) < json.IndexOf("zeta", StringComparison.Ordinal));
    }

    /// <summary>
    /// A value containing a comma and a quote must survive JSON encoding — the CSV layer then quotes the
    /// whole column, so the two encodings compose without corrupting the row.
    /// </summary>
    [Fact]
    public void FormatMetadata_EscapesAwkwardValues()
    {
        var json = AuditLogView.FormatMetadata(new Dictionary<string, string>
        {
            ["note"] = "a, b \"c\""
        });

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Assert.Equal("a, b \"c\"", parsed["note"]);
    }
}

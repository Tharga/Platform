using Tharga.Team.Service.Audit;

namespace Tharga.Platform.Sample.Framework;

/// <summary>
/// Demo <see cref="IAuditEnricher"/> that stamps host-defined metadata onto every audit entry the
/// toolkit writes. A real app would add correlation ids, ticket references, tenant tags, and so on —
/// pulled from <c>IHttpContextAccessor</c> or its own services. Here it adds a couple of static keys
/// plus the current request's trace id so the enrichment is visible in the /audit detail row.
/// </summary>
public class SampleAuditEnricher(IHttpContextAccessor httpContextAccessor) : IAuditEnricher
{
    public void Enrich(AuditEntry entry, IDictionary<string, string> metadata)
    {
        metadata["sample.enricher"] = "hello";

        var traceId = httpContextAccessor.HttpContext?.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            metadata["request.trace-id"] = traceId;
        }
    }
}

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Adds host-defined metadata to audit entries the toolkit writes. Register with
/// <c>AddThargaAuditEnricher&lt;T&gt;()</c>.
/// </summary>
/// <remarks>
/// Invoked by <see cref="CompositeAuditLogger"/> for every entry that passes the audit filters, just
/// before the entry is dispatched to the sinks. Because that logger is a singleton and its
/// <see cref="IAuditLogger.Log"/> is synchronous, an enricher must be resolvable as a singleton and
/// must not block; read per-request state through <c>IHttpContextAccessor</c>, exactly as the rest of
/// the audit pipeline does.
/// <para>
/// Merge is <b>add-only</b>: keys the toolkit already set on the entry win, and the first enricher to
/// write a given key wins over later ones. An enricher therefore augments the record; it cannot rewrite
/// what the toolkit recorded. An enricher that throws is logged and skipped — enrichment can never fail
/// the operation being audited.
/// </para>
/// </remarks>
public interface IAuditEnricher
{
    /// <summary>
    /// Adds entries to <paramref name="metadata"/>. <paramref name="entry"/> is the entry so far
    /// (including any toolkit-set metadata) and is read-only in effect — mutations to
    /// <paramref name="metadata"/> are the only output.
    /// </summary>
    void Enrich(AuditEntry entry, IDictionary<string, string> metadata);
}

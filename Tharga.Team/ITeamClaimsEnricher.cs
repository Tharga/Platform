using System.Security.Claims;

namespace Tharga.Team;

/// <summary>
/// Implement to inject custom claims (e.g. global roles) before team consent evaluation.
/// Register via <c>AddClaimsEnricher&lt;T&gt;()</c> on <c>ThargaBlazorOptions</c>.
/// Called once per request inside TeamServerClaimsTransformation, before member lookup and consent check.
/// </summary>
public interface ITeamClaimsEnricher
{
    Task EnrichAsync(ClaimsIdentity identity);
}

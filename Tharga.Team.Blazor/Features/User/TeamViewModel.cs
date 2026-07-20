using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// View model for a team row in the teams list view.
/// </summary>
public record TeamViewModel
{
    public string Key { get; init; }
    public string Name { get; init; }
    public int MemberCount { get; init; }
    public TeamMemberInfo[] Members { get; init; }

    /// <summary>
    /// What this team has consented to grant an oversight caller. Only meaningful to a caller holding
    /// the <c>teams:read</c> system scope; otherwise every listed team is one the caller belongs to.
    /// </summary>
    public ConsentVisibility Consent { get; init; }
}

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
}

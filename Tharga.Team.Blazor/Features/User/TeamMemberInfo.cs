namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Member info for a team row drill-down.
/// </summary>
public record TeamMemberInfo
{
    public string Key { get; init; }
    public string Name { get; init; }
    public string EMail { get; init; }
    public AccessLevel AccessLevel { get; init; }
    public MembershipState? State { get; init; }
    public DateTime? LastSeen { get; init; }
}

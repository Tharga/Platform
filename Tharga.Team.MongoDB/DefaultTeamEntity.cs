namespace Tharga.Team.MongoDB;

/// <summary>
/// The standard stored team, holding <see cref="DefaultTeamMember"/> members.
/// </summary>
/// <remarks>
/// Empty for the same reason <see cref="DefaultTeamMember"/> is: <see cref="TeamEntityBase{TTeamMemberModel}"/>
/// carries the whole shape, and the subclass existed only to pin the member type.
/// <para>
/// Declare your own deriving from <see cref="TeamEntityBase{TTeamMemberModel}"/> when the team itself
/// needs extra properties, or when you are using a member type of your own.
/// </para>
/// </remarks>
public record DefaultTeamEntity : TeamEntityBase<DefaultTeamMember>;

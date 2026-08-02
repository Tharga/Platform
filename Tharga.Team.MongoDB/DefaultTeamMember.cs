namespace Tharga.Team.MongoDB;

/// <summary>
/// The standard team member. Use this unless a member needs properties of your own.
/// </summary>
/// <remarks>
/// <b>Named <c>Default…</c> rather than <c>TeamMember</c> deliberately.</b> Nearly every existing host
/// already declares its own <c>TeamMember</c>, and a type of that name here would collide with it in any
/// file importing this namespace — breaking compilation for precisely the hosts this was meant to help.
/// The toolkit's own sample caught it immediately.
/// </remarks>
/// <remarks>
/// <b>It is empty on purpose.</b> <see cref="TeamMemberBase"/> already carries everything the toolkit
/// needs — key, name, invitation, access level, roles, scope overrides, suspension — and until this type
/// existed every host had to declare its own empty subclass to supply a type argument. That declaration
/// expressed no decision, so the toolkit now makes it.
/// <para>
/// To add properties, declare your own record deriving from <see cref="TeamMemberBase"/> and name it on
/// the three-argument <c>RegisterTeamService</c> overload. That overload exists for exactly this.
/// </para>
/// </remarks>
public record DefaultTeamMember : TeamMemberBase;

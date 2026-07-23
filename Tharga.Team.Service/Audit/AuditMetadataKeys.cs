namespace Tharga.Team.Service.Audit;

/// <summary>
/// Metadata keys the toolkit writes onto <see cref="AuditEntry.Metadata"/>.
/// </summary>
/// <remarks>
/// Named rather than inlined so the vocabulary stays consistent across decorators and so a consumer can
/// filter or display on a stable key. Values are dotted and lower-case; a change here is a change to the
/// audit record's shape, so treat these as part of the public contract.
/// <para>
/// A <c>.old</c> / <c>.new</c> pair is written only where the previous value is needed to interpret the
/// entry (rename, consent level, member access level, member display name). Operations whose identity is
/// the whole story — invite, remove — record the subject only, so they cost no extra read.
/// </para>
/// </remarks>
public static class AuditMetadataKeys
{
    /// <summary>Team display name, on create.</summary>
    public const string TeamName = "team.name";

    /// <summary>Team name before a rename.</summary>
    public const string TeamNameOld = "team.name.old";

    /// <summary>Team name after a rename.</summary>
    public const string TeamNameNew = "team.name.new";

    /// <summary>Member the operation acted on.</summary>
    public const string MemberKey = "member.key";

    /// <summary>Email a member was invited with.</summary>
    public const string MemberEmail = "member.email";

    /// <summary>Member access level before a change.</summary>
    public const string MemberAccessLevelOld = "member.accesslevel.old";

    /// <summary>Member access level after a change.</summary>
    public const string MemberAccessLevelNew = "member.accesslevel.new";

    /// <summary>Per-team member display name before a change. Empty string means "no override".</summary>
    public const string MemberNameOld = "member.name.old";

    /// <summary>Per-team member display name after a change. Empty string means the override was cleared.</summary>
    public const string MemberNameNew = "member.name.new";

    /// <summary>Tenant roles assigned to a member, comma-separated.</summary>
    public const string MemberTenantRoles = "member.tenantroles";

    /// <summary>Per-member scope overrides, comma-separated.</summary>
    public const string MemberScopeOverrides = "member.scopeoverrides";

    /// <summary>Consented access level before a change. Absent value means "no consent".</summary>
    public const string ConsentAccessLevelOld = "consent.accesslevel.old";

    /// <summary>Consented access level after a change. Absent value means consent was cleared.</summary>
    public const string ConsentAccessLevelNew = "consent.accesslevel.new";

    /// <summary>Roles the team consented to, comma-separated.</summary>
    public const string ConsentRoles = "consent.roles";

    /// <summary>Custom (runtime-defined) tenant role names, comma-separated.</summary>
    public const string CustomRoleNames = "customroles.names";

    /// <summary>New owner on an ownership transfer.</summary>
    public const string NewOwnerKey = "team.newowner.key";

    /// <summary>Number of teams a user was removed from, on a remove-from-all-teams operation.</summary>
    public const string MemberTeamCount = "member.teamcount";
}

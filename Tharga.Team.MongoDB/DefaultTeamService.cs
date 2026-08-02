using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

/// <summary>
/// The standard team service, storing <see cref="DefaultTeamEntity"/> teams with
/// <see cref="DefaultTeamMember"/> members. Register it and write no team storage code at all.
/// </summary>
/// <remarks>
/// The two members it implements are the only ones
/// <see cref="TeamServiceRepositoryBase{TTeamEntity,TMember}"/> leaves abstract, and neither carries a
/// decision: they exist because a base class cannot <c>new</c> a generic type parameter it does not
/// control, so every host was writing the same two object initializers.
/// <para>
/// <b>Everything stays overridable.</b> Derive from this to change any of it — the twenty-odd storage
/// members are virtual on the base, and both factories below are virtual too. Use
/// <see cref="TeamServiceRepositoryBase{TTeamEntity,TMember}"/> directly instead when the team or member
/// needs properties of your own; that is what the three-argument <c>RegisterTeamService</c> overload is
/// for.
/// </para>
/// </remarks>
public class DefaultTeamService : TeamServiceRepositoryBase<DefaultTeamEntity, DefaultTeamMember>
{
    public DefaultTeamService(
        IUserService userService,
        ITeamRepository<DefaultTeamEntity, DefaultTeamMember> teamRepository,
        IMongoDbServiceFactory mongoDbServiceFactory,
        IIconStore iconStore = null)
        : base(userService, teamRepository, mongoDbServiceFactory, iconStore)
    {
    }

    /// <remarks>
    /// <paramref name="teamKey"/> is supplied by <c>TeamServiceBase</c>, which generates it and has
    /// already checked it is unused — this method does not invent one.
    /// </remarks>
    protected override Task<DefaultTeamEntity> CreateTeam(string teamKey, string name, IUser user, string displayName)
    {
        return Task.FromResult(new DefaultTeamEntity
        {
            Key = teamKey,
            Name = name,
            Members =
            [
                new DefaultTeamMember
                {
                    Key = user.Key,
                    Name = displayName,
                    AccessLevel = AccessLevel.Owner,
                    State = MembershipState.Member
                }
            ]
        });
    }

    /// <remarks>
    /// The member starts with a null <c>Key</c> and an invitation: they are not a member of anything
    /// until they accept, and the key is theirs, not one this service may invent for them.
    /// </remarks>
    protected override Task<DefaultTeamMember> CreateTeamMember(InviteUserModel model)
    {
        return Task.FromResult(new DefaultTeamMember
        {
            Key = null,
            Name = model.Name,
            Invitation = new Invitation
            {
                EMail = model.Email,
                InviteKey = Guid.NewGuid().ToString(),
                InviteTime = DateTime.UtcNow
            },
            State = MembershipState.Invited,
            AccessLevel = model.AccessLevel
        });
    }
}

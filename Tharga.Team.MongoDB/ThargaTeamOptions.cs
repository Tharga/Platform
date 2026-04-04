using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

public record ThargaTeamOptions
{
    internal Type _userEntity;
    internal Type _teamEntity;
    internal Type _teamMemberModel;

    /// <summary>
    /// MongoDB collection name for team documents. Default is "Team".
    /// </summary>
    public string TeamCollectionName { get; set; } = "Team";

    /// <summary>
    /// MongoDB collection name for user documents. Default is "User".
    /// </summary>
    public string UserCollectionName { get; set; } = "User";

    public void RegisterUserRepository<TUserEntity>()
        where TUserEntity : EntityBase, IUser
    {
        _userEntity = typeof(TUserEntity);
    }

    public void RegisterTeamRepository<TTeamEntity, TTeamMemberModel>()
        where TTeamEntity : TeamEntityBase<TTeamMemberModel>
        where TTeamMemberModel : TeamMemberBase
    {
        _teamEntity = typeof(TTeamEntity);
        _teamMemberModel = typeof(TTeamMemberModel);
    }
}
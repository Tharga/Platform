using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

public record ThargaTeamOptions
{
    internal Type _userEntity;
    internal Type _userCollectionType;
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

    /// <summary>
    /// Registers the User repository using the built-in <see cref="UserRepositoryCollection{TUserEntity}"/>.
    /// Use the <c>RegisterUserRepository&lt;TUserEntity, TCollection&gt;</c> overload to register a consumer
    /// subclass that declares additional per-deployment indices.
    /// </summary>
    public void RegisterUserRepository<TUserEntity>()
        where TUserEntity : EntityBase, IUser
    {
        _userEntity = typeof(TUserEntity);
        _userCollectionType = null;
    }

    /// <summary>
    /// Registers the User repository with a consumer-provided collection subclass.
    /// Use this when you need to add per-deployment indices on top of the built-in
    /// unique <c>Identity</c> index (e.g. a unique index on a custom email field).
    /// </summary>
    public void RegisterUserRepository<TUserEntity, TCollection>()
        where TUserEntity : EntityBase, IUser
        where TCollection : UserRepositoryCollection<TUserEntity>
    {
        _userEntity = typeof(TUserEntity);
        _userCollectionType = typeof(TCollection);
    }

    public void RegisterTeamRepository<TTeamEntity, TTeamMemberModel>()
        where TTeamEntity : TeamEntityBase<TTeamMemberModel>
        where TTeamMemberModel : TeamMemberBase
    {
        _teamEntity = typeof(TTeamEntity);
        _teamMemberModel = typeof(TTeamMemberModel);
    }
}
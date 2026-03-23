using Tharga.Blazor.Framework;

namespace Tharga.Team.Blazor.Framework;

public record ThargaBlazorOptions : BlazorOptions
{
    internal Type _teamService;
    internal Type _userService;
    internal Type _memberType;
    internal Type _apiKeyService;

    /// <summary>
    /// Automatically create the first team for users.
    /// Default is false.
    /// </summary>
    public bool AutoCreateFirstTeam { get; set; } = false;

    /// <summary>
    /// Show member role management in the team component.
    /// Default is false.
    /// </summary>
    public bool ShowMemberRoles { get; set; } = false;

    /// <summary>
    /// Show individual scope overrides in the team component.
    /// Default is false.
    /// </summary>
    public bool ShowScopeOverrides { get; set; } = false;

    /// <summary>
    /// When true, skips decorating AuthenticationStateProvider with
    /// TeamClaimsAuthenticationStateProvider. Use this for SSR-based apps
    /// that use IClaimsTransformation instead of JS-based claim augmentation.
    /// Default is false.
    /// </summary>
    public bool SkipAuthStateDecoration { get; set; } = false;

    /// <summary>
    /// Add types for team and user services.
    /// </summary>
    /// <typeparam name="TServiceBase"></typeparam>
    /// <typeparam name="TUserService"></typeparam>
    public void RegisterTeamService<TServiceBase, TUserService>()
        where TServiceBase : TeamServiceBase
        where TUserService : UserServiceBase
    {
        _teamService = typeof(TServiceBase);
        _userService = typeof(TUserService);
    }

    public void RegisterTeamService<TServiceBase, TUserService, TMember>()
        where TServiceBase : TeamServiceBase
        where TUserService : UserServiceBase
        where TMember : class, ITeamMember
    {
        _teamService = typeof(TServiceBase);
        _userService = typeof(TUserService);
        _memberType = typeof(TMember);
    }

    public void RegisterApiKeyAdministrationService<TApiKeyService>()
        where TApiKeyService : IApiKeyAdministrationService
    {
        _apiKeyService = typeof(TApiKeyService);
    }
}
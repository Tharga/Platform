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
    /// Allow users to create and delete teams via the UI.
    /// When false, the "Create team" and "Delete team" buttons are hidden.
    /// Independent of AutoCreateFirstTeam (system behavior).
    /// Default is true.
    /// </summary>
    public bool AllowTeamCreation { get; set; } = true;

    /// <summary>
    /// Controls how team/scope claims are enriched on the principal.
    /// <para>
    /// <b>true (default)</b> — Claims are enriched server-side via <c>IClaimsTransformation</c>,
    /// which reads the <c>selected_team_id</c> cookie. Works for Blazor Server, SSR, and Hybrid apps.
    /// No JS interop is used. This is the recommended setting for most applications.
    /// </para>
    /// <para>
    /// <b>false</b> — Additionally registers a client-side <c>AuthenticationStateProvider</c> decorator
    /// that enriches claims via LocalStorage/JS interop. Only needed for standalone Blazor WebAssembly
    /// apps with no server-side HTTP pipeline. Setting this to false on a Server/SSR app will cause
    /// a blank page (silent deadlock from JS interop during prerendering).
    /// </para>
    /// </summary>
    public bool SkipAuthStateDecoration { get; set; } = true;

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
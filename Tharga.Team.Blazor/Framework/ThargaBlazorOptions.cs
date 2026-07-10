using Tharga.Blazor.Framework;
using Tharga.Team;

namespace Tharga.Team.Blazor.Framework;

public record ThargaBlazorOptions : BlazorOptions
{
    internal Type _teamService;
    internal Type _userService;
    internal Type _memberType;
    internal Type _apiKeyService;
    internal Type _claimsEnricher;
    internal Type _textProvider;

    /// <summary>
    /// Automatically create the first team for users.
    /// Default is false.
    /// </summary>
    public bool AutoCreateFirstTeam { get; set; } = false;

    /// <summary>
    /// Allow users to create and delete teams via the UI.
    /// When false, the "Create team" and "Delete team" buttons are hidden.
    /// Independent of AutoCreateFirstTeam (system behavior).
    /// Default is true.
    /// </summary>
    public bool AllowTeamCreation { get; set; } = true;

    /// <summary>
    /// Optional route that the built-in "Create team" entry points navigate to instead of
    /// performing the bare create. When set, the teamless "Create team" link in
    /// <c>TeamSelector</c> and the "Create new Team" button in <c>TeamComponent</c> redirect
    /// here — letting a host route team creation into its own onboarding flow while keeping
    /// <see cref="AllowTeamCreation"/> <c>true</c> so the programmatic create API still works.
    /// A per-component <c>CreateTeamRequested</c> callback, when supplied, takes precedence
    /// over this path. When <c>null</c> (default), the built-in behavior is unchanged.
    /// </summary>
    public string CreateTeamPath { get; set; }

    /// <summary>
    /// Data-access consent options (cross-team access granted by a team to global roles).
    /// </summary>
    public ConsentOptions Consent { get; set; } = new();

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

    /// <summary>
    /// Register a custom claims enricher that runs before team member lookup and consent evaluation.
    /// Use this to inject global roles or other claims from external sources (e.g. database).
    /// </summary>
    public void AddClaimsEnricher<TEnricher>()
        where TEnricher : class, ITeamClaimsEnricher
    {
        _claimsEnricher = typeof(TEnricher);
    }

    /// <summary>
    /// Register a custom <see cref="IThargaTextProvider"/> to localize Tharga.Team UI strings (e.g. the
    /// profile menu and team selector). Overrides the built-in English default; when none is registered,
    /// the English defaults are used.
    /// </summary>
    public void AddTextProvider<TProvider>()
        where TProvider : class, IThargaTextProvider
    {
        _textProvider = typeof(TProvider);
    }
}
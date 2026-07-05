using Tharga.Team;
using Tharga.Team.Blazor.Features.Authentication;
using Tharga.Team.Service;
using Tharga.Team.Service.Audit;
using Tharga.Team.Service.Email;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Top-level options for configuring the Tharga Platform via a single AddThargaPlatform call.
/// All sub-options have sensible defaults. Configure only what you need.
/// </summary>
public class ThargaPlatformOptions
{
    internal Type _emailSenderType;
    internal readonly List<Type> _apiKeyLifecycleHandlers = [];

    /// <summary>
    /// Options for the Blazor UI layer (title, team service types, auth state decoration).
    /// </summary>
    public ThargaBlazorOptions Blazor { get; } = new();

    /// <summary>
    /// Options for Azure AD / OIDC authentication.
    /// </summary>
    public ThargaAuthOptions Auth { get; } = new();

    /// <summary>
    /// Options for API key authentication scheme.
    /// Set to null to skip API key authentication registration.
    /// </summary>
    public ApiKeyOptions ApiKey { get; set; } = new();

    /// <summary>
    /// Options for MVC controllers and Swagger. Set to null to skip.
    /// </summary>
    public ThargaControllerOptions Controllers { get; set; } = new();

    /// <summary>
    /// Configure scopes for access-level-based authorization.
    /// When null, scope registration is skipped and scope UI columns are hidden.
    /// </summary>
    public Action<ScopeRegistry> ConfigureScopes { get; set; }

    /// <summary>
    /// Configure tenant roles for role-based authorization within teams.
    /// When null, tenant role registration is skipped and role UI columns are hidden.
    /// </summary>
    public Action<TenantRoleRegistry> ConfigureTenantRoles { get; set; }

    /// <summary>
    /// Enable runtime-defined (dynamic) tenant roles: team administrators can create / update / delete their
    /// own custom roles per team (scopes constrained to app-registered scopes), managed via the
    /// <c>TenantRoleManager</c> component and surfaced alongside code-registered roles. When false (default),
    /// only code-registered roles apply and custom-role scopes are not resolved into claims.
    /// </summary>
    public bool EnableDynamicRoles { get; set; }

    /// <summary>
    /// Configure system-level (global) scopes — the capabilities offered to system API keys (and, via role
    /// mapping, to privileged users). When null, system-scope registration is skipped.
    /// </summary>
    public Action<SystemScopeRegistry> ConfigureSystemScopes { get; set; }

    /// <summary>
    /// Map app/global roles (e.g. "Developer") to system scopes, so privileged users gain those scopes as
    /// claims (team-independent). When null, no role→system-scope mapping is applied.
    /// </summary>
    public Action<SystemRoleRegistry> ConfigureSystemRoles { get; set; }

    /// <summary>
    /// Options for audit logging. Set to null to skip audit registration.
    /// </summary>
    public AuditOptions Audit { get; set; }

    /// <summary>
    /// Options for email sending (SMTP). When set and no custom email service is registered,
    /// the built-in SmtpTeamEmailSender is used. Set to null to disable email.
    /// </summary>
    public EmailOptions Email { get; set; }

    /// <summary>
    /// Register a custom email sender implementation. When set, this takes precedence
    /// over the built-in SMTP sender regardless of EmailOptions.
    /// </summary>
    public void AddEmailService<T>() where T : class, ITeamEmailSender
    {
        _emailSenderType = typeof(T);
    }

    /// <summary>
    /// Register a handler that receives an API key's private token on create and recycle/regenerate,
    /// plus a tokenless signal on delete (see <see cref="IApiKeyLifecycleHandler"/>). May be called
    /// multiple times to register several handlers. Requires <see cref="ApiKey"/> to be set.
    /// </summary>
    public void AddApiKeyLifecycleHandler<THandler>() where THandler : class, IApiKeyLifecycleHandler
    {
        _apiKeyLifecycleHandlers.Add(typeof(THandler));
    }
}

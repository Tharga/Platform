using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Team.Service.Audit;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Authentication handler that validates API keys from the X-API-KEY header.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyAdministrationService _apiKeyAdministrationService;
    private readonly IScopeRegistry _scopeRegistry;
    private readonly IAuditLogger _auditLogger;

    /// <summary>
    /// Creates a new instance of the API key authentication handler.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyAdministrationService apiKeyAdministrationService,
        IScopeRegistry scopeRegistry = null,
        CompositeAuditLogger auditLogger = null)
        : base(options, logger, encoder)
    {
        _apiKeyAdministrationService = apiKeyAdministrationService;
        _scopeRegistry = scopeRegistry;
        _auditLogger = auditLogger;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.NoResult();

        var key = await _apiKeyAdministrationService.GetByApiKeyAsync(apiKey);
        if (key == null)
        {
            LogAuthEvent(null, null, false, "Invalid API key");
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, key.Name ?? key.TeamKey ?? "system"),
        };

        if (key.TeamKey == null)
        {
            // System key: explicit scopes, no team claim
            claims.Add(new Claim(TeamClaimTypes.IsSystemKey, "true"));
            foreach (var scope in key.SystemScopes ?? Array.Empty<string>())
            {
                claims.Add(new Claim(TeamClaimTypes.Scope, scope));
            }
        }
        else
        {
            // Team key: resolve scopes through registry
            var (accessLevel, roleNames, scopeOverrides) = ResolveKeyDetails(key);
            claims.Add(new Claim(TeamClaimTypes.TeamKey, key.TeamKey));
            claims.Add(new Claim(TeamClaimTypes.AccessLevel, accessLevel.ToString()));

            if (_scopeRegistry != null)
            {
                foreach (var scope in _scopeRegistry.GetEffectiveScopes(accessLevel, roleNames, scopeOverrides))
                {
                    claims.Add(new Claim(TeamClaimTypes.Scope, scope));
                }
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        LogAuthEvent(key.Name ?? key.TeamKey ?? "system", key.TeamKey, true);

        return AuthenticateResult.Success(ticket);
    }

    private void LogAuthEvent(string callerIdentity, string teamKey, bool success, string errorMessage = null)
    {
        _auditLogger?.Log(new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = success ? AuditEventType.AuthSuccess : AuditEventType.AuthFailure,
            Feature = "auth",
            Action = "apikey",
            MethodName = "HandleAuthenticateAsync",
            Success = success,
            ErrorMessage = errorMessage,
            CallerType = AuditCallerType.ApiKey,
            CallerIdentity = callerIdentity,
            TeamKey = teamKey,
            CallerSource = AuditCallerSource.Api,
        });
    }

    private static (AccessLevel accessLevel, string[] roleNames, string[] scopeOverrides) ResolveKeyDetails(IApiKey key)
    {
        if (key is ApiKeyEntity entity)
        {
            var al = entity.AccessLevel ?? AccessLevel.Administrator;
            var roles = entity.Roles ?? Array.Empty<string>();
            var overrides = entity.ScopeOverrides ?? Array.Empty<string>();
            return (al, roles, overrides);
        }

        var accessLevelStr = key.Tags.TryGetValue(TeamClaimTypes.AccessLevel, out var level)
            ? level
            : AccessLevel.Viewer.ToString();

        var accessLevel = Enum.TryParse<AccessLevel>(accessLevelStr, out var parsed)
            ? parsed
            : AccessLevel.Viewer;

        var roleStr = key.Tags.TryGetValue("TenantRoles", out var r)
            ? r.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        return (accessLevel, roleStr, Array.Empty<string>());
    }
}

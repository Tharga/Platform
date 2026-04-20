using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Extension methods for registering API key authentication.
/// </summary>
public static class ApiKeyRegistration
{
    /// <summary>
    /// Adds API key authentication with the default <see cref="ApiKeyAdministrationService"/>.
    /// </summary>
    public static AuthenticationBuilder AddThargaApiKeyAuthentication(this AuthenticationBuilder builder, Action<ApiKeyOptions> configureOptions = null)
    {
        return builder.AddThargaApiKeyAuthentication<ApiKeyAdministrationService>(configureOptions);
    }

    /// <summary>
    /// Adds API key authentication with a custom <see cref="IApiKeyAdministrationService"/> implementation.
    /// </summary>
    public static AuthenticationBuilder AddThargaApiKeyAuthentication<TService>(this AuthenticationBuilder builder, Action<ApiKeyOptions> configureOptions = null)
        where TService : class, IApiKeyAdministrationService
    {
        if (configureOptions != null)
            builder.Services.Configure(configureOptions);
        else
            builder.Services.Configure<ApiKeyOptions>(_ => { });

        builder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyConstants.SchemeName, "API Key", null);

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiKeyConstants.PolicyName, policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyConstants.SchemeName);
                policy.RequireAuthenticatedUser();
                // Team policy rejects system keys — a team key has no IsSystemKey claim set to "true"
                policy.RequireAssertion(ctx =>
                    !ctx.User.HasClaim(TeamClaimTypes.IsSystemKey, "true"));
            });

            options.AddPolicy(ApiKeyConstants.SystemPolicyName, policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyConstants.SchemeName);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(TeamClaimTypes.IsSystemKey, "true");
            });
        });

        builder.Services.AddScoped<IApiKeyAdministrationService, TService>();

        return builder;
    }
}

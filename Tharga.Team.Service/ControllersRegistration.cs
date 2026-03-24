using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Tharga.Toolkit.Password;

namespace Tharga.Team.Service;

/// <summary>
/// Extension methods for registering API controllers with OpenAPI and Swagger.
/// </summary>
public static class ControllersRegistration
{
    /// <summary>
    /// Registers MVC controllers, OpenAPI document with API key security scheme, Swagger, and endpoints API explorer.
    /// </summary>
    public static IServiceCollection AddThargaControllers(this IServiceCollection services, Action<ThargaControllerOptions> configure = null)
    {
        var options = new ThargaControllerOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddControllers();

#if NET10_0_OR_GREATER
        services.AddOpenApi(o =>
        {
            o.AddDocumentTransformer((document, _, _) =>
            {
                var schemes = document.Components?.SecuritySchemes
                              ?? new Dictionary<string, IOpenApiSecurityScheme>();

                schemes[ApiKeyConstants.OpenApiSchemeId] = new OpenApiSecurityScheme
                {
                    Name = ApiKeyConstants.HeaderName,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "API key for authentication"
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = schemes;

                document.Security ??= [];
                var requirement = new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(ApiKeyConstants.OpenApiSchemeId, document)] = []
                };
                document.Security.Add(requirement);

                return Task.CompletedTask;
            });
        });
#endif

        services.AddSwaggerGen(o =>
        {
            o.AddSecurityDefinition(ApiKeyConstants.OpenApiSchemeId, new OpenApiSecurityScheme
            {
                Name = ApiKeyConstants.HeaderName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API key for authentication"
            });
            o.AddSecurityRequirement(document =>
            {
                var requirement = new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(ApiKeyConstants.OpenApiSchemeId, document)] = []
                };
                return requirement;
            });
        });
        services.AddEndpointsApiExplorer();

        return services;
    }

    /// <summary>
    /// Registers the default API key storage, administration service, and hashing service.
    /// Also registers the MongoDB repository types explicitly so they are available
    /// regardless of the entry assembly name prefix used by the consumer.
    /// </summary>
    public static IServiceCollection AddThargaApiKeys(this IServiceCollection services)
    {
        services.RegisterApiKeyService();
        services.AddScoped<IApiKeyManagementService, ApiKeyManagementService>();
        services.AddTransient<IApiKeyRepository, ApiKeyRepository>();
        services.AddTransient<IApiKeyRepositoryCollection, ApiKeyRepositoryCollection>();
        return services;
    }

    /// <summary>
    /// Maps controllers, OpenAPI endpoint, and Swagger UI.
    /// </summary>
    public static WebApplication UseThargaControllers(this WebApplication app)
    {
        var options = app.Services.GetService<ThargaControllerOptions>() ?? new ThargaControllerOptions();

        app.MapControllers();
#if NET10_0_OR_GREATER
        app.MapOpenApi();
#endif
        app.UseSwaggerUI(o =>
        {
            o.RoutePrefix = options.SwaggerRoutePrefix;
            o.SwaggerEndpoint("/openapi/v1.json", options.SwaggerTitle);
        });

        return app;
    }
}

#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
#endif

namespace Tharga.Team.Service;

/// <summary>
/// Options for configuring Tharga controller registration.
/// </summary>
public class ThargaControllerOptions
{
    /// <summary>
    /// Title shown in Swagger UI. Defaults to "API v1".
    /// </summary>
    public string SwaggerTitle { get; set; } = "API v1";

    /// <summary>
    /// Swagger UI route prefix. Defaults to "swagger".
    /// </summary>
    public string SwaggerRoutePrefix { get; set; } = "swagger";

#if NET10_0_OR_GREATER
    internal Action<OpenApiOptions> OpenApiConfigure { get; private set; }

    /// <summary>
    /// Registers additional OpenAPI configuration — such as an
    /// <see cref="IOpenApiDocumentTransformer"/> or <see cref="IOpenApiOperationTransformer"/> — that
    /// is applied to the same OpenAPI document Tharga manages via <c>AddThargaControllers</c>.
    /// </summary>
    /// <param name="configure">
    /// A callback that receives the <see cref="OpenApiOptions"/> Tharga passes to
    /// <c>AddOpenApi</c>. Use it to add document/operation transformers
    /// (e.g. <c>api =&gt; api.AddDocumentTransformer&lt;ScopeFilteringDocumentTransformer&gt;()</c>).
    /// </param>
    /// <returns>The same <see cref="ThargaControllerOptions"/> instance so calls can be chained.</returns>
    /// <remarks>
    /// Multiple calls compose — every registered callback runs, in registration order, against
    /// Tharga's document. Prefer this over calling <c>AddOpenApi("v1", …)</c> directly: it keeps
    /// the consumer's transformers on the document Tharga already manages and avoids the .NET 10
    /// OpenAPI XML-comment source generator emitting an interceptor into the consumer project.
    /// Available on .NET 10 and later only; on .NET 9 the Swashbuckle-based document is used and this
    /// hook is not present.
    /// </remarks>
    public ThargaControllerOptions ConfigureOpenApi(Action<OpenApiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        OpenApiConfigure += configure;
        return this;
    }
#endif
}

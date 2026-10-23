using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JDB_Portfolio_API.Authentication;

/// <summary>
/// Marks only the endpoints that really require a token as requiring one.
/// </summary>
/// <remarks>
/// This replaces a global <c>AddSecurityRequirement</c>, which told every reader — and
/// every generated client — that the whole API needs a bearer token. The portfolio
/// endpoints are anonymous on purpose (they feed a public CV site), so documenting them
/// as protected is worse than documenting nothing: it invites a client to authenticate
/// against endpoints that will never check, and it hides the one route that does.
/// </remarks>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresAuthorization = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer", context.Document)] = new List<string>()
        });
    }
}

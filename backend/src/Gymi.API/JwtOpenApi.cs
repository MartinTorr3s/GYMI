using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Gymi.API;

internal static class JwtOpenApi
{
    public static Task AgregarEsquema(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken ct)
    {
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        return Task.CompletedTask;
    }

    public static Task MarcarOperacionProtegida(OpenApiOperation operacion, OpenApiOperationTransformerContext contexto, CancellationToken ct)
    {
        var metadatos = contexto.Description.ActionDescriptor.EndpointMetadata;
        if (metadatos.OfType<IAuthorizeData>().Any() && !metadatos.OfType<IAllowAnonymous>().Any())
        {
            operacion.Security ??= [];
            operacion.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, contexto.Document)] = []
            });
        }
        return Task.CompletedTask;
    }
}

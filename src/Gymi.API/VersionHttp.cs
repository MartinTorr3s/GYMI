using Gymi.UseCase;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace Gymi.API;

internal static class VersionHttp
{
    private sealed class IfMatchObligatorio { }

    public static RouteHandlerBuilder DocumentarIfMatch(this RouteHandlerBuilder endpoint, IServiceProvider servicios)
    {
        endpoint.ProducesProblem(StatusCodes.Status412PreconditionFailed);
        if (!servicios.GetRequiredService<IConfiguration>().GetValue<bool>("Edicion:RequerirIfMatch"))
        {
            return endpoint;
        }

        return endpoint.WithMetadata(new IfMatchObligatorio())
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .AddOpenApiOperationTransformer((operacion, _, _) =>
            {
                operacion.Parameters ??= [];
                operacion.Parameters.Add(new OpenApiParameter
                {
                    Name = HeaderNames.IfMatch,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Copiá el ETag de la ficha, incluidas las comillas.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                });
                return Task.CompletedTask;
            });
    }

    public static IResult ConETag(HttpContext contexto, Guid version, IResult resultado)
    {
        contexto.Response.Headers.ETag = Formatear(version);
        return resultado;
    }

    public static Task<IResult> ActualizarAsync<T>(HttpContext contexto,
        Func<Guid?, Task<Resultado<T>>> actualizar, Func<T, Guid> versionDe) =>
        ConVersionAsync(contexto, actualizar,
            ficha => ConETag(contexto, versionDe(ficha), Results.Ok(ficha)));

    public static async Task<IResult> ConVersionAsync<T>(HttpContext contexto,
        Func<Guid?, Task<Resultado<T>>> ejecutar, Func<T, IResult> exito)
    {
        var cabecera = contexto.Request.Headers.IfMatch;
        Guid? version = null;
        if (cabecera.Count > 0)
        {
            var ifMatch = cabecera[0];
            if (cabecera.Count != 1 || ifMatch is not { Length: 38 } || ifMatch[0] != '"' || ifMatch[^1] != '"'
                || !Guid.TryParseExact(ifMatch.AsSpan(1, 36), "D", out var esperada)
                || esperada == Guid.Empty || !string.Equals(ifMatch, Formatear(esperada), StringComparison.Ordinal))
            {
                return ErroresHttp.Crear(contexto, StatusCodes.Status400BadRequest, "version_invalida",
                    "Enviá un único ETag válido en If-Match, incluidas las comillas.");
            }

            version = esperada;
        }
        else if (contexto.GetEndpoint()?.Metadata.GetMetadata<IfMatchObligatorio>() is not null)
        {
            return ErroresHttp.Crear(contexto, StatusCodes.Status428PreconditionRequired, "version_requerida",
                "Enviá el ETag de la ficha en If-Match.");
        }

        return ErroresHttp.DesdeResultado(await ejecutar(version), contexto, exito);
    }

    private static string Formatear(Guid version) => $"\"{version:D}\"";
}

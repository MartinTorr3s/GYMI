namespace Gymi.API;

internal static class ConsultaHttp
{
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>>
        ValidarParametros(params string[] permitidos)
    {
        var nombres = new HashSet<string>(permitidos, StringComparer.OrdinalIgnoreCase);
        var mensaje = $"Usá únicamente {string.Join(", ", permitidos)}.";
        return (contexto, siguiente) =>
        {
            var http = contexto.HttpContext;
            return http.Request.Query.Keys.Any(clave => !nombres.Contains(clave))
                ? ValueTask.FromResult<object?>(ErroresHttp.Crear(http, StatusCodes.Status400BadRequest,
                    "parametros_desconocidos", mensaje))
                : siguiente(contexto);
        };
    }
}

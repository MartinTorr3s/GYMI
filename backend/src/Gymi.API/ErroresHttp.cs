using System.Diagnostics;
using Gymi.UseCase;
using Microsoft.AspNetCore.Diagnostics;

namespace Gymi.API;

internal static class ErroresHttp
{
    public static IResult DesdeResultado<T>(Resultado<T> resultado, HttpContext contexto, Func<T, IResult> exito) =>
        resultado.Error is { } error
            ? Crear(contexto, error.Tipo switch
            {
                TipoError.Validacion => StatusCodes.Status400BadRequest,
                TipoError.Autenticacion => StatusCodes.Status401Unauthorized,
                TipoError.NoEncontrado => StatusCodes.Status404NotFound,
                TipoError.Precondicion => StatusCodes.Status412PreconditionFailed,
                TipoError.Conflicto => StatusCodes.Status409Conflict,
                _ => throw new ArgumentOutOfRangeException(nameof(resultado))
            }, error.Codigo, error.Mensaje, error.Campos, error.Detalles)
            : exito(resultado.Valor!);

    public static IResult Crear(HttpContext contexto, int status, string codigo, string mensaje,
        Dictionary<string, string[]>? campos = null, IReadOnlyDictionary<string, object?>? detalles = null)
    {
        var extensiones = new Dictionary<string, object?>
        {
            ["code"] = codigo,
            ["traceId"] = ObtenerTraceId(contexto)
        };

        if (campos is not null)
        {
            extensiones["errors"] = campos;
        }

        if (detalles is not null)
        {
            foreach (var (clave, valor) in detalles)
            {
                extensiones.TryAdd(clave, valor);
            }
        }

        return Results.Problem(statusCode: status, title: mensaje, extensions: extensiones);
    }

    public static string ObtenerTraceId(HttpContext contexto) =>
        Activity.Current?.Id ?? contexto.TraceIdentifier;

    public static IResult DesdeEstado(HttpContext contexto, int estado) => estado switch
    {
        StatusCodes.Status400BadRequest =>
            Crear(contexto, estado, "solicitud_invalida", "Revisá el cuerpo o los parámetros de la solicitud."),
        StatusCodes.Status401Unauthorized =>
            Crear(contexto, estado, "autenticacion_requerida", "Iniciá sesión para continuar."),
        StatusCodes.Status403Forbidden =>
            Crear(contexto, estado, "permiso_denegado", "Tu cuenta no tiene permiso para esta operación."),
        StatusCodes.Status429TooManyRequests =>
            Crear(contexto, estado, "demasiados_intentos", "Esperá antes de volver a intentar."),
        StatusCodes.Status404NotFound =>
            Crear(contexto, estado, "ruta_no_encontrada", "No se encontró la ruta solicitada."),
        StatusCodes.Status405MethodNotAllowed =>
            Crear(contexto, estado, "metodo_no_permitido", "El método HTTP no está permitido para esta ruta."),
        StatusCodes.Status413PayloadTooLarge =>
            Crear(contexto, estado, "solicitud_demasiado_grande", "El cuerpo de la solicitud supera el tamaño permitido."),
        StatusCodes.Status415UnsupportedMediaType =>
            Crear(contexto, estado, "formato_no_admitido", "Enviá el cuerpo con el tipo application/json."),
        _ => Crear(contexto, estado, "solicitud_rechazada", "No se pudo procesar la solicitud.")
    };
}

internal sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> logger,
    IErroresPersistencia persistencia) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        if (excepcion is BadHttpRequestException peticion)
        {
            await ErroresHttp.DesdeEstado(contexto, peticion.StatusCode).ExecuteAsync(contexto);
            return true;
        }

        if (persistencia.EsContencionTemporal(excepcion))
        {
            contexto.Response.Headers.RetryAfter = "1";
            await ErroresHttp.Crear(contexto, StatusCodes.Status503ServiceUnavailable, "base_ocupada",
                "La base está ocupada. Volvé a intentar en unos segundos.").ExecuteAsync(contexto);
            return true;
        }

        logger.LogError("Error inesperado de tipo {Tipo}. TraceId {TraceId}",
            excepcion.GetType().Name, ErroresHttp.ObtenerTraceId(contexto));

        await ErroresHttp.Crear(contexto, StatusCodes.Status500InternalServerError, "error_interno",
            "Ocurrió un error al procesar la solicitud.").ExecuteAsync(contexto);
        return true;
    }
}

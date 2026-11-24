using System.Text.Json.Nodes;
using Gymi.UseCase;

namespace Gymi.API.Controllers;

internal static class EndpointsAsistencias
{
    public static void MapearAsistencias(this IEndpointRouteBuilder app)
    {
        app.MapPost("/asistencias/consultar-cliente", ConsultarClienteAsync)
            .WithTags("Asistencias")
            .RequireAuthorization(RolesGymi.Personal)
            .WithSummary("Buscar al cliente por DNI")
            .WithDescription("Devuelve la vigencia, el saldo y si el cliente puede ingresar.")
            .AddOpenApiOperationTransformer((operacion, _, _) =>
            {
                if (operacion.RequestBody?.Content?.TryGetValue("application/json", out var cuerpo) == true)
                {
                    cuerpo.Example = new JsonObject { ["dni"] = "00123456" };
                }

                return Task.CompletedTask;
            })
            .Produces<ConsultaAsistenciaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        var grupo = app.MapGroup("/clientes/{clienteId}/asistencias").WithTags("Asistencias").RequireAuthorization(RolesGymi.Personal);
        grupo.MapPost("", RegistrarAsync)
            .WithSummary("Registrar un ingreso y descontar una sesión")
            .WithDescription("Generá un UUID por cada ingreso y reutilizalo si necesitás reenviar esa misma operación.")
            .AddOpenApiOperationTransformer((operacion, _, _) =>
            {
                if (operacion.RequestBody?.Content?.TryGetValue("application/json", out var cuerpo) == true)
                {
                    cuerpo.Example = new JsonObject { ["operacionId"] = Guid.NewGuid().ToString("D") };
                }

                return Task.CompletedTask;
            })
            .Produces<AsistenciaDto>(StatusCodes.Status201Created)
            .Produces<AsistenciaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{asistenciaId}", ObtenerAsync)
            .WithName("ObtenerAsistencia")
            .WithSummary("Consultar el comprobante de una asistencia")
            .Produces<AsistenciaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ConsultarClienteAsync(DatosConsultaAsistencia datos,
        IAsistenciasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ConsultarClienteAsync(datos, ct), contexto, consulta => Results.Ok(consulta));

    private static async Task<IResult> RegistrarAsync(int clienteId, DatosRegistroAsistencia datos,
        IAsistenciasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.RegistrarAsync(clienteId, datos, ct), contexto,
            confirmacion => confirmacion.EsReintento ? Results.Ok(confirmacion.Asistencia)
                : Results.CreatedAtRoute("ObtenerAsistencia", new { clienteId, asistenciaId = confirmacion.Asistencia.Id },
                    confirmacion.Asistencia));

    private static async Task<IResult> ObtenerAsync(int clienteId, int asistenciaId,
        IAsistenciasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ObtenerAsync(clienteId, asistenciaId, ct), contexto,
            asistencia => Results.Ok(asistencia));
}

using System.Text.Json.Nodes;
using Gymi.UseCase;

namespace Gymi.API.Controllers;

internal static class EndpointsMembresias
{
    public static void MapearMembresias(this IEndpointRouteBuilder app)
    {
        app.MapGet("/membresias/vencidas", ListarVencidasAsync)
            .AddEndpointFilter(ConsultaHttp.ValidarParametros("pagina", "tamanoPagina"))
            .WithTags("Alertas")
            .RequireAuthorization(RolesGymi.Personal)
            .WithSummary("Consultar las membresías vencidas")
            .WithDescription("Devuelve una alerta por cliente activo sin período vigente, desde el vencimiento más reciente.")
            .Produces<PaginaAlertasVencimiento>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        var grupo = app.MapGroup("/clientes/{clienteId}/membresias").WithTags("Membresias").RequireAuthorization(RolesGymi.Personal);

        grupo.MapPost("", AsignarAsync)
            .WithSummary("Asignar un plan a un cliente")
            .WithDescription("Indicá el ID de un plan activo y la fecha de inicio. GYMI calcula el vencimiento y el saldo inicial.")
            .AddOpenApiOperationTransformer((operacion, _, _) =>
            {
                if (operacion.RequestBody?.Content?.TryGetValue("application/json", out var cuerpo) == true)
                {
                    cuerpo.Example = new JsonObject { ["planId"] = 1, ["fechaInicio"] = "2027-01-31" };
                }

                return Task.CompletedTask;
            })
            .Produces<MembresiaDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("", ListarAsync)
            .WithSummary("Listar las membresías de un cliente")
            .WithDescription("Incluye las membresías programadas, vigentes y vencidas, desde la fecha de inicio más reciente.")
            .Produces<PaginaMembresias>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{id}", ObtenerAsync)
            .WithName("ObtenerMembresia")
            .WithSummary("Consultar la vigencia y el saldo de una membresía")
            .Produces<MembresiaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ListarVencidasAsync(int? pagina, int? tamanoPagina,
        IMembresiasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ListarVencidasAsync(pagina ?? 1, tamanoPagina ?? 25, ct),
            contexto, alertas => Results.Ok(alertas));

    private static async Task<IResult> AsignarAsync(int clienteId, DatosAsignacionMembresia datos,
        IMembresiasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.AsignarAsync(clienteId, datos, ct), contexto,
            membresia => Results.CreatedAtRoute("ObtenerMembresia", new { clienteId, id = membresia.Id }, membresia));

    private static async Task<IResult> ListarAsync(int clienteId, int? pagina, int? tamanoPagina,
        IMembresiasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ListarAsync(clienteId, pagina ?? 1, tamanoPagina ?? 25, ct),
            contexto, membresias => Results.Ok(membresias));

    private static async Task<IResult> ObtenerAsync(int clienteId, int id,
        IMembresiasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ObtenerAsync(clienteId, id, ct), contexto,
            membresia => Results.Ok(membresia));
}

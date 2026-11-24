using System.Text.Json.Nodes;
using Gymi.UseCase;

namespace Gymi.API.Controllers;

internal static class EndpointsPagos
{
    public static void MapearPagos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/clientes/{clienteId}/membresias/{membresiaId}/pagos").WithTags("Pagos").RequireAuthorization(RolesGymi.Personal);

        grupo.MapPost("", RegistrarAsync)
            .WithSummary("Registrar un pago de membresía")
            .WithDescription("Elegí primerPago para cubrir la membresía indicada o renovacion para crear el próximo período.")
            .AddOpenApiOperationTransformer((operacion, _, _) =>
            {
                if (operacion.RequestBody?.Content?.TryGetValue("application/json", out var cuerpo) == true)
                {
                    cuerpo.Example = new JsonObject
                    {
                        ["tipo"] = "primerPago",
                        ["fechaPago"] = "2026-11-01",
                        ["monto"] = 25000m,
                        ["medioPago"] = "Transferencia"
                    };
                }

                return Task.CompletedTask;
            })
            .Produces<PagoDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{pagoId}", ObtenerAsync)
            .WithName("ObtenerPago")
            .WithSummary("Consultar un pago y el período que cubre")
            .Produces<PagoDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> RegistrarAsync(int clienteId, int membresiaId, DatosRegistroPago datos,
        IPagosController controller, LinkGenerator enlaces, HttpContext contexto, CancellationToken ct)
    {
        var resultado = await controller.RegistrarAsync(clienteId, membresiaId, datos, ct);
        if (resultado.Error?.Detalles is { } detalles &&
            detalles.TryGetValue("pagoId", out var id) && id is int pagoId &&
            detalles.TryGetValue("membresiaOrigenId", out var origen) && origen is int origenId)
        {
            contexto.Response.Headers.Location = enlaces.GetPathByName(contexto, "ObtenerPago",
                new { clienteId, membresiaId = origenId, pagoId });
        }

        return ErroresHttp.DesdeResultado(resultado, contexto,
            pago => Results.CreatedAtRoute("ObtenerPago", new { clienteId, membresiaId, pagoId = pago.Id }, pago));
    }

    private static async Task<IResult> ObtenerAsync(int clienteId, int membresiaId, int pagoId,
        IPagosController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ObtenerAsync(clienteId, membresiaId, pagoId, ct), contexto,
            pago => Results.Ok(pago));
}

using System.Globalization;
using System.Security.Claims;
using Gymi.UseCase;
using Microsoft.AspNetCore.Mvc;

namespace Gymi.API.Controllers;

internal static class EndpointsReportes
{
    public static void MapearReportes(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/reportes").WithTags("Reportes").RequireAuthorization(RolesGymi.Administrador);
        grupo.AddEndpointFilter(ConsultaHttp.ValidarParametros("fechaDesde", "fechaHasta", "pagina", "tamanoPagina", "cursor"));

        grupo.MapGet("/clientes-activos", ClientesActivosAsync)
            .WithSummary("Consultar el reporte de clientes activos")
            .WithDescription("El período opcional filtra la fecha de alta de las fichas que están activas al consultar.")
            .DocumentarReporte<FilaClienteActivo>();

        grupo.MapGet("/pagos-registrados", PagosRegistradosAsync)
            .WithSummary("Consultar el reporte de pagos registrados")
            .WithDescription("El período opcional filtra la fecha del pago. Incluye el historial de clientes desactivados. "
                + "Usá clienteId, membresiaOrigenId y pagoId para consultar el comprobante. "
                + "Para una renovación, membresiaOrigenId identifica el período que se renovó.")
            .DocumentarReporte<FilaPagoRegistrado>();

        grupo.MapGet("/asistencias", AsistenciasAsync)
            .WithSummary("Consultar el reporte de asistencias")
            .WithDescription("El período opcional usa la fecha local del ingreso en Buenos Aires. La hora se devuelve en UTC.")
            .DocumentarReporte<FilaAsistencia>();
    }

    private static RouteHandlerBuilder DocumentarReporte<T>(this RouteHandlerBuilder endpoint) =>
        endpoint.Produces<ReporteDto<T>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

    private sealed record ParametrosReporte(
        [FromQuery(Name = "fechaDesde")] string? FechaDesde,
        [FromQuery(Name = "fechaHasta")] string? FechaHasta,
        [FromQuery(Name = "pagina")] int? Pagina,
        [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
        [FromQuery(Name = "cursor")] Guid? Cursor)
    {
        public DatosReporte Datos(HttpContext contexto) =>
            new(int.Parse(contexto.User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture),
                FechaDesde, FechaHasta, Pagina, TamanoPagina, Cursor);
    }

    private static async Task<IResult> ClientesActivosAsync([AsParameters] ParametrosReporte parametros,
        IReportesController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ClientesActivosAsync(parametros.Datos(contexto), ct), contexto,
            reporte => Results.Ok(reporte));

    private static async Task<IResult> PagosRegistradosAsync([AsParameters] ParametrosReporte parametros,
        IReportesController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.PagosRegistradosAsync(parametros.Datos(contexto), ct), contexto,
            reporte => Results.Ok(reporte));

    private static async Task<IResult> AsistenciasAsync([AsParameters] ParametrosReporte parametros,
        IReportesController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.AsistenciasAsync(parametros.Datos(contexto), ct), contexto,
            reporte => Results.Ok(reporte));
}

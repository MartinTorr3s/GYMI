using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class ReportesController(IReportesEntrada entrada, IPresentador<ReporteDto<FilaClienteActivo>> clientes,
    IPresentador<ReporteDto<FilaPagoRegistrado>> pagos, IPresentador<ReporteDto<FilaAsistencia>> asistencias) : IReportesController
{
    public async Task<Resultado<ReporteDto<FilaClienteActivo>>> ClientesActivosAsync(DatosReporte datos, CancellationToken ct)
    {
        await entrada.ClientesActivosAsync(datos, ct);
        return clientes.Resultado;
    }

    public async Task<Resultado<ReporteDto<FilaPagoRegistrado>>> PagosRegistradosAsync(DatosReporte datos, CancellationToken ct)
    {
        await entrada.PagosRegistradosAsync(datos, ct);
        return pagos.Resultado;
    }

    public async Task<Resultado<ReporteDto<FilaAsistencia>>> AsistenciasAsync(DatosReporte datos, CancellationToken ct)
    {
        await entrada.AsistenciasAsync(datos, ct);
        return asistencias.Resultado;
    }
}

public interface IReportesController
{
    Task<Resultado<ReporteDto<FilaClienteActivo>>> ClientesActivosAsync(DatosReporte datos, CancellationToken ct);
    Task<Resultado<ReporteDto<FilaPagoRegistrado>>> PagosRegistradosAsync(DatosReporte datos, CancellationToken ct);
    Task<Resultado<ReporteDto<FilaAsistencia>>> AsistenciasAsync(DatosReporte datos, CancellationToken ct);
}

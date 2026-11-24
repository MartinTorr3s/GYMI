namespace Gymi.UseCase;

public interface IReportesEntrada
{
    Task ClientesActivosAsync(DatosReporte datos, CancellationToken ct);
    Task PagosRegistradosAsync(DatosReporte datos, CancellationToken ct);
    Task AsistenciasAsync(DatosReporte datos, CancellationToken ct);
}

public interface ICursoresReporte
{
    ContinuacionReporte? Buscar(Guid cursor);
    Guid Guardar(ContinuacionReporte continuacion);
}

public interface IReporteRepository
{
    Task<int> LeerIdMaximoAsync(TipoReporte tipo, CancellationToken ct);
    Task<IReadOnlyList<FilaClienteActivo>> ListarClientesActivosAsync(FiltroReporte filtro, CancellationToken ct);
    Task<IReadOnlyList<FilaPagoRegistrado>> ListarPagosAsync(FiltroReporte filtro, CancellationToken ct);
    Task<IReadOnlyList<FilaAsistencia>> ListarAsistenciasAsync(FiltroReporte filtro, DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtcExclusiva, CancellationToken ct);
}

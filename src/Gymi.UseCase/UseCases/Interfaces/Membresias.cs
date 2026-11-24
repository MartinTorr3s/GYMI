using Gymi.Entity;

namespace Gymi.UseCase;

public interface IMembresiasEntrada
{
    Task AsignarAsync(int clienteId, DatosAsignacionMembresia datos, CancellationToken ct);
    Task ObtenerAsync(int clienteId, int id, CancellationToken ct);
    Task ListarAsync(int clienteId, int pagina, int tamanoPagina, CancellationToken ct);
    Task ListarVencidasAsync(int pagina, int tamanoPagina, CancellationToken ct);
}

public interface IMembresiaRepository
{
    Task<EstadoAsignacionMembresia> GuardarAsignacionAsync(Membresia membresia, Guid versionPlan, CancellationToken ct);
    Task<Membresia?> BuscarAsync(int clienteId, int id, CancellationToken ct);
    Task<IReadOnlyList<Membresia>> ListarAsync(int clienteId, int offset, int cantidad, CancellationToken ct);
    Task<IReadOnlyList<AlertaVencimientoDto>> ListarVencidasAsync(DateOnly fecha, int offset, int cantidad, CancellationToken ct);
}

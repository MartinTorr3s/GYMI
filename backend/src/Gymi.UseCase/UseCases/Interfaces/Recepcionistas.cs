using Gymi.Entity;

namespace Gymi.UseCase;

public interface IRecepcionistasEntrada
{
    Task RegistrarAsync(DatosRecepcionista datos, CancellationToken ct);
    Task ObtenerAsync(int id, CancellationToken ct);
    Task ListarAsync(bool? activo, int pagina, int tamanoPagina, CancellationToken ct);
    Task ModificarAsync(int id, DatosRecepcionista datos, Guid? version, CancellationToken ct);
    Task DesactivarAsync(int id, Guid? version, CancellationToken ct);
}

public interface IRecepcionistaRepository
{
    void Agregar(Recepcionista recepcionista);
    Task<Recepcionista?> BuscarAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Recepcionista>> ListarAsync(bool? activo, int offset, int cantidad, CancellationToken ct);
    Task<EstadoGuardadoRecepcionista> GuardarAsync(CancellationToken ct);
}

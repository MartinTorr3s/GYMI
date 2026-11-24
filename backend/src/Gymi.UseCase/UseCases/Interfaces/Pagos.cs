using Gymi.Entity;

namespace Gymi.UseCase;

public interface IPagosEntrada
{
    Task RegistrarAsync(int clienteId, int membresiaId, DatosRegistroPago datos, CancellationToken ct);
    Task ObtenerAsync(int clienteId, int membresiaId, int pagoId, CancellationToken ct);
}

public interface IPagoRepository
{
    Task<RegistroPago> RegistrarAsync(int clienteId, int membresiaId, PagoValidado datos, CancellationToken ct);
    Task<Pago?> BuscarAsync(int clienteId, int membresiaId, int pagoId, CancellationToken ct);
}

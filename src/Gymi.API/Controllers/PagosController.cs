using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class PagosController(IPagosEntrada entrada, IPresentador<PagoDto> salida) : IPagosController
{
    public async Task<Resultado<PagoDto>> RegistrarAsync(int clienteId, int membresiaId, DatosRegistroPago datos, CancellationToken ct)
    {
        await entrada.RegistrarAsync(clienteId, membresiaId, datos, ct);
        return salida.Resultado;
    }

    public async Task<Resultado<PagoDto>> ObtenerAsync(int clienteId, int membresiaId, int pagoId, CancellationToken ct)
    {
        await entrada.ObtenerAsync(clienteId, membresiaId, pagoId, ct);
        return salida.Resultado;
    }
}

public interface IPagosController
{
    Task<Resultado<PagoDto>> RegistrarAsync(int clienteId, int membresiaId, DatosRegistroPago datos, CancellationToken ct);
    Task<Resultado<PagoDto>> ObtenerAsync(int clienteId, int membresiaId, int pagoId, CancellationToken ct);
}

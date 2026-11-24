using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class MembresiasController(IMembresiasEntrada entrada, IPresentador<MembresiaDto> ficha,
    IPresentador<PaginaMembresias> pagina, IPresentador<PaginaAlertasVencimiento> alertas) : IMembresiasController
{
    public async Task<Resultado<MembresiaDto>> AsignarAsync(int clienteId, DatosAsignacionMembresia datos, CancellationToken ct)
    {
        await entrada.AsignarAsync(clienteId, datos, ct);
        return ficha.Resultado;
    }

    public async Task<Resultado<MembresiaDto>> ObtenerAsync(int clienteId, int id, CancellationToken ct)
    {
        await entrada.ObtenerAsync(clienteId, id, ct);
        return ficha.Resultado;
    }

    public async Task<Resultado<PaginaMembresias>> ListarAsync(int clienteId, int numero, int tamano, CancellationToken ct)
    {
        await entrada.ListarAsync(clienteId, numero, tamano, ct);
        return pagina.Resultado;
    }

    public async Task<Resultado<PaginaAlertasVencimiento>> ListarVencidasAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        await entrada.ListarVencidasAsync(pagina, tamanoPagina, ct);
        return alertas.Resultado;
    }
}

public interface IMembresiasController
{
    Task<Resultado<MembresiaDto>> AsignarAsync(int clienteId, DatosAsignacionMembresia datos, CancellationToken ct);
    Task<Resultado<MembresiaDto>> ObtenerAsync(int clienteId, int id, CancellationToken ct);
    Task<Resultado<PaginaMembresias>> ListarAsync(int clienteId, int pagina, int tamanoPagina, CancellationToken ct);
    Task<Resultado<PaginaAlertasVencimiento>> ListarVencidasAsync(int pagina, int tamanoPagina, CancellationToken ct);
}

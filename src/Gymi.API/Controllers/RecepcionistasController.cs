using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class RecepcionistasController(IRecepcionistasEntrada entrada, IPresentador<RecepcionistaDto> ficha,
    IPresentador<PaginaRecepcionistas> listado, IPresentador<Guid> baja) : IRecepcionistasController
{
    public async Task<Resultado<RecepcionistaDto>> RegistrarAsync(DatosRecepcionista datos, CancellationToken ct)
    {
        await entrada.RegistrarAsync(datos, ct);
        return ficha.Resultado;
    }

    public async Task<Resultado<RecepcionistaDto>> ObtenerAsync(int id, CancellationToken ct)
    {
        await entrada.ObtenerAsync(id, ct);
        return ficha.Resultado;
    }

    public async Task<Resultado<PaginaRecepcionistas>> ListarAsync(bool? activo, int pagina, int tamanoPagina, CancellationToken ct)
    {
        await entrada.ListarAsync(activo, pagina, tamanoPagina, ct);
        return listado.Resultado;
    }

    public async Task<Resultado<RecepcionistaDto>> ModificarAsync(int id, DatosRecepcionista datos, Guid? version, CancellationToken ct)
    {
        await entrada.ModificarAsync(id, datos, version, ct);
        return ficha.Resultado;
    }

    public async Task<Resultado<Guid>> DesactivarAsync(int id, Guid? version, CancellationToken ct)
    {
        await entrada.DesactivarAsync(id, version, ct);
        return baja.Resultado;
    }
}

public interface IRecepcionistasController
{
    Task<Resultado<RecepcionistaDto>> RegistrarAsync(DatosRecepcionista datos, CancellationToken ct);
    Task<Resultado<RecepcionistaDto>> ObtenerAsync(int id, CancellationToken ct);
    Task<Resultado<PaginaRecepcionistas>> ListarAsync(bool? activo, int pagina, int tamanoPagina, CancellationToken ct);
    Task<Resultado<RecepcionistaDto>> ModificarAsync(int id, DatosRecepcionista datos, Guid? version, CancellationToken ct);
    Task<Resultado<Guid>> DesactivarAsync(int id, Guid? version, CancellationToken ct);
}

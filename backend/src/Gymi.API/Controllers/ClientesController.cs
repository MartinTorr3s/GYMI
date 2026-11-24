using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class ClientesController(
    IClientesEntrada entrada,
    IPresentador<ClienteDto> cliente,
    IPresentador<PaginaClientes> pagina,
    IPresentador<Guid> baja) : IClientesController
{
    public async Task<Resultado<ClienteDto>> RegistrarAsync(DatosCliente datos, CancellationToken ct)
    {
        await entrada.RegistrarAsync(datos, ct);
        return cliente.Resultado;
    }

    public async Task<Resultado<ClienteDto>> ObtenerAsync(int id, CancellationToken ct)
    {
        await entrada.ObtenerAsync(id, ct);
        return cliente.Resultado;
    }

    public async Task<Resultado<PaginaClientes>> ListarAsync(int numero, int tamano, string? busqueda, CancellationToken ct)
    {
        await entrada.ListarAsync(numero, tamano, busqueda, ct);
        return pagina.Resultado;
    }

    public async Task<Resultado<ClienteDto>> ModificarAsync(int id, DatosCliente datos, Guid? version, CancellationToken ct)
    {
        await entrada.ModificarAsync(id, datos, version, ct);
        return cliente.Resultado;
    }

    public async Task<Resultado<Guid>> DesactivarAsync(int id, Guid? version, CancellationToken ct)
    {
        await entrada.DesactivarAsync(id, version, ct);
        return baja.Resultado;
    }
}

public interface IClientesController
{
    Task<Resultado<ClienteDto>> RegistrarAsync(DatosCliente datos, CancellationToken ct);
    Task<Resultado<ClienteDto>> ObtenerAsync(int id, CancellationToken ct);
    Task<Resultado<PaginaClientes>> ListarAsync(int pagina, int tamanoPagina, string? busqueda, CancellationToken ct);
    Task<Resultado<ClienteDto>> ModificarAsync(int id, DatosCliente datos, Guid? version, CancellationToken ct);
    Task<Resultado<Guid>> DesactivarAsync(int id, Guid? version, CancellationToken ct);
}

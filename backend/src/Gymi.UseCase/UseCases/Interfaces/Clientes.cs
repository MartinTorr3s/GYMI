using Gymi.Entity;

namespace Gymi.UseCase;

public interface IClientesEntrada
{
    Task RegistrarAsync(DatosCliente datos, CancellationToken ct);
    Task ObtenerAsync(int id, CancellationToken ct);
    Task ListarAsync(int pagina, int tamanoPagina, string? busqueda, CancellationToken ct);
    Task ModificarAsync(int id, DatosCliente datos, Guid? version, CancellationToken ct);
    Task DesactivarAsync(int id, Guid? version, CancellationToken ct);
}

public interface IPuertoSalida<T>
{
    void Presentar(Resultado<T> resultado);
}

public interface IPresentador<T> : IPuertoSalida<T>
{
    Resultado<T> Resultado { get; }
}

public interface IClienteRepository
{
    Task<Cliente?> BuscarAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Cliente>> ListarActivosAsync(int offset, int cantidad, string? busqueda, CancellationToken ct);
    Task<bool> ExisteDniAsync(string dni, int? idExceptuado, CancellationToken ct);
    void Agregar(Cliente cliente);
    Task<EstadoGuardadoCliente> GuardarAsync(CancellationToken ct);
}

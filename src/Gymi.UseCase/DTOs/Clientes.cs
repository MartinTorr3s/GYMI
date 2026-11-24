using Gymi.Entity;

namespace Gymi.UseCase;

public static class LimitesCliente
{
    public const int Nombre = 60;
    public const int Apellido = 60;
    public const int Email = 120;
    public const int Dni = 8;
    public const int TelefonoDigitos = 15;
}

public sealed record DatosCliente(string? Nombre, string? Apellido, string? Email, string? Dni, string? Telefono);
public sealed record ClienteDto(int Id, string Nombre, string Apellido, string Email, string Dni, string Telefono,
    DateOnly FechaAlta, bool Activo, Guid Version)
{
    public static ClienteDto Desde(Cliente cliente) =>
        new(cliente.Id, cliente.Nombre, cliente.Apellido, cliente.Email, cliente.Dni, cliente.Telefono,
            cliente.FechaAlta, cliente.Activo, cliente.Version);
}

public sealed record PaginaClientes(IReadOnlyList<ClienteDto> Clientes, int Pagina, int TamanoPagina, bool HayMas);

public enum TipoError
{
    Validacion,
    Autenticacion,
    NoEncontrado,
    Conflicto,
    Precondicion
}

public sealed record ErrorAplicacion(TipoError Tipo, string Codigo, string Mensaje,
    Dictionary<string, string[]>? Campos = null, IReadOnlyDictionary<string, object?>? Detalles = null);

public sealed class Resultado<T>
{
    private Resultado(T? valor, ErrorAplicacion? error)
    {
        Valor = valor;
        Error = error;
    }

    public T? Valor { get; }
    public ErrorAplicacion? Error { get; }

    public static Resultado<T> Exito(T valor) => new(valor, null);
    public static Resultado<T> Fallo(ErrorAplicacion error) => new(default, error);
}

public enum EstadoGuardadoCliente
{
    Guardado,
    DniDuplicado,
    EmailAccesoDuplicado,
    CambioConcurrente
}

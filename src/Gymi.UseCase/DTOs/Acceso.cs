using Gymi.Entity;

namespace Gymi.UseCase;

public sealed record DatosInicioSesion(string? Email, string? Contrasena);
public sealed record DatosActivacion(string? Token, string? Contrasena);
public sealed record DatosPrimerAdministrador(string? Nombre, string? Apellido, string? Email, string? Contrasena);
public sealed record UsuarioSesionDto(int Id, string Nombre, string Apellido, string Rol)
{
    public static UsuarioSesionDto Desde(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Apellido, RolesGymi.Nombre(usuario.Rol));
}
public sealed record SesionAbierta(UsuarioSesionDto Usuario, string IdSesion, DateTimeOffset EmitidaEn, DateTimeOffset VenceEn);
public sealed record InvitacionActivacion(string Token, DateTimeOffset VenceEn);
public sealed record MiMembresiaDto(ClienteDto Cliente, MembresiaDto? Membresia, string? NombrePlan);
public sealed record PagoCuentaDto(DateOnly FechaPago, decimal Monto, string MedioPago);
public sealed record PaginaPagosCuenta(IReadOnlyList<PagoCuentaDto> Pagos, int Pagina, int TamanoPagina, bool HayMas);

public static class RolesGymi
{
    public const string Administrador = "administrador";
    public const string Recepcionista = "recepcionista";
    public const string Cliente = "cliente";
    public const string Personal = "personal";

    public static string Nombre(RolUsuario rol) => rol switch
    {
        RolUsuario.Administrador => Administrador,
        RolUsuario.Recepcionista => Recepcionista,
        RolUsuario.Cliente => Cliente,
        _ => throw new ArgumentOutOfRangeException(nameof(rol))
    };
}

public enum TipoInvitacion
{
    Cliente,
    Recepcionista
}

public enum EstadoCuenta
{
    Guardado,
    ClienteInexistente,
    ClienteInactivo,
    RecepcionistaInexistente,
    RecepcionistaInactivo,
    AccesoYaHabilitado,
    EmailOcupado,
    ActivacionInvalida,
    AdministradorExistente
}

public sealed record ConsultaMembresiaPropia(Cliente Cliente, Membresia? Membresia, string? NombrePlan);

using Gymi.Entity;

namespace Gymi.UseCase;

public interface IAccesoEntrada
{
    Task IniciarSesionAsync(DatosInicioSesion datos, CancellationToken ct);
    Task InvitarAsync(int id, TipoInvitacion tipo, CancellationToken ct);
    Task ActivarAsync(DatosActivacion datos, CancellationToken ct);
    Task MiMembresiaAsync(int clienteId, CancellationToken ct);
    Task MisPagosAsync(int clienteId, int pagina, int tamanoPagina, CancellationToken ct);
}

public interface IControlDeSesion
{
    Task<UsuarioSesionDto?> ValidarAsync(string? idSesion, CancellationToken ct);
    Task CerrarAsync(string? idSesion, CancellationToken ct);
}

public interface IProvisionAdministrador
{
    Task<Resultado<bool>> CrearPrimeroAsync(DatosPrimerAdministrador datos, CancellationToken ct);
}

public interface IProteccionContrasenas
{
    string CrearHash(string contrasena);
    bool Verificar(string? hash, string contrasena);
}

public interface IAccesoRepository
{
    Task<Usuario?> BuscarUsuarioAsync(string emailNormalizado, CancellationToken ct);
    Task<bool> CrearSesionAsync(SesionUsuario sesion, DateTimeOffset ahora, CancellationToken ct);
    Task<Usuario?> BuscarSesionAsync(string tokenHash, DateTimeOffset ahora, CancellationToken ct);
    Task CerrarSesionAsync(string tokenHash, CancellationToken ct);
    Task<EstadoCuenta> GuardarInvitacionAsync(int id, TipoInvitacion tipo, string tokenHash, DateTimeOffset venceEn, CancellationToken ct);
    Task<EstadoCuenta> ActivarAsync(string tokenHash, string passwordHash, DateTimeOffset ahora, CancellationToken ct);
    Task<EstadoCuenta> CrearPrimerAdministradorAsync(Administrador administrador, CancellationToken ct);
    Task<ConsultaMembresiaPropia?> BuscarMembresiaPropiaAsync(int clienteId, DateOnly fecha, CancellationToken ct);
    Task<IReadOnlyList<PagoCuentaDto>> BuscarPagosPropiosAsync(int clienteId, int offset, int cantidad, CancellationToken ct);
}

using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

internal static class ContrasenaEspecificacion
{
    private const int LongitudMinimaNueva = 8;
    private const int LongitudMaxima = 128;

    public static bool TieneFormato(string? contrasena) =>
        contrasena is { Length: >= 1 and <= LongitudMaxima } && !contrasena.Any(char.IsControl);

    public static void ValidarNueva(string? contrasena, Dictionary<string, string[]> errores)
    {
        if (!TieneFormato(contrasena) || contrasena!.Length < LongitudMinimaNueva || !contrasena.Any(char.IsLetter)
            || !contrasena.Any(c => char.IsDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c)))
        {
            errores["contrasena"] = [$"Usá entre {LongitudMinimaNueva} y {LongitudMaxima} caracteres, con alguna letra y al menos un número o símbolo."];
        }
    }
}

public sealed class ControlDeSesion(IAccesoRepository repository, TimeProvider reloj) : IControlDeSesion
{
    public async Task<UsuarioSesionDto?> ValidarAsync(string? idSesion, CancellationToken ct)
    {
        if (!TokensAcceso.EsValido(idSesion))
        {
            return null;
        }

        var usuario = await repository.BuscarSesionAsync(TokensAcceso.Huella(idSesion!), reloj.GetUtcNow(), ct);
        return usuario is null ? null : UsuarioSesionDto.Desde(usuario);
    }

    public Task CerrarAsync(string? idSesion, CancellationToken ct) =>
        TokensAcceso.EsValido(idSesion) ? repository.CerrarSesionAsync(TokensAcceso.Huella(idSesion!), ct) : Task.CompletedTask;
}

public sealed class ProvisionAdministrador(IAccesoRepository repository, IProteccionContrasenas contrasenas) : IProvisionAdministrador
{
    public async Task<Resultado<bool>> CrearPrimeroAsync(DatosPrimerAdministrador datos, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        var nombre = ValidacionEntrada.Texto(datos.Nombre, "nombre", "El nombre", LimitesCliente.Nombre, errores);
        var apellido = ValidacionEntrada.Texto(datos.Apellido, "apellido", "El apellido", LimitesCliente.Apellido, errores);
        var email = ClienteEspecificacion.ValidarEmail(datos.Email, errores);
        ContrasenaEspecificacion.ValidarNueva(datos.Contrasena, errores);
        if (errores.Count > 0)
        {
            return Resultado<bool>.Fallo(ValidacionEntrada.Error(errores));
        }

        var administrador = new Administrador(nombre, apellido, email);
        administrador.HabilitarAcceso(contrasenas.CrearHash(datos.Contrasena!));
        return await repository.CrearPrimerAdministradorAsync(administrador, ct) switch
        {
            EstadoCuenta.Guardado => Resultado<bool>.Exito(true),
            EstadoCuenta.AdministradorExistente => Resultado<bool>.Fallo(new(TipoError.Conflicto,
                "administrador_existente", "Ya hay un administrador con acceso configurado.")),
            EstadoCuenta.EmailOcupado => Resultado<bool>.Fallo(new(TipoError.Conflicto,
                "email_acceso_ocupado", "El correo ya está asociado al acceso de otra cuenta.")),
            _ => throw new InvalidOperationException("Estado de provisión inesperado.")
        };
    }
}

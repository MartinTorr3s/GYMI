using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class AccesoInteractor(IAccesoRepository repository, IProteccionContrasenas contrasenas,
    IPresentador<SesionAbierta> sesionSalida, IPresentador<InvitacionActivacion> invitacionSalida,
    IPresentador<bool> activacionSalida, IPresentador<MiMembresiaDto> membresiaSalida,
    IPresentador<PaginaPagosCuenta> pagosSalida, TimeProvider reloj, TimeZoneInfo zonaHoraria) : IAccesoEntrada
{
    private static readonly ErrorAplicacion CredencialesInvalidas =
        new(TipoError.Autenticacion, "credenciales_invalidas", "El correo o la contraseña son incorrectos.");

    public async Task IniciarSesionAsync(DatosInicioSesion datos, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        // Las cuentas existentes pueden iniciar sesión con un dominio guardado antes de la validación estricta.
        var email = ClienteEspecificacion.ValidarEmail(datos.Email, errores, exigirDominioValido: false);
        if (!ContrasenaEspecificacion.TieneFormato(datos.Contrasena))
        {
            errores["contrasena"] = ["Ingresá una contraseña válida de hasta 128 caracteres."];
        }

        if (errores.Count > 0)
        {
            sesionSalida.Presentar(Resultado<SesionAbierta>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var usuario = await repository.BuscarUsuarioAsync(Usuario.NormalizarEmail(email), ct);
        var hash = usuario is { AccesoWebHabilitado: true } ? usuario.PasswordHash : null;
        var coincide = contrasenas.Verificar(hash, datos.Contrasena!);
        if (!coincide || usuario is not { AccesoWebHabilitado: true })
        {
            sesionSalida.Presentar(Resultado<SesionAbierta>.Fallo(CredencialesInvalidas));
            return;
        }

        var ahora = reloj.GetUtcNow();
        var emitidaEn = DateTimeOffset.FromUnixTimeSeconds(ahora.ToUnixTimeSeconds());
        var venceEn = emitidaEn.AddHours(8);
        var idSesion = TokensAcceso.Crear();
        var sesion = new SesionUsuario(usuario, TokensAcceso.Huella(idSesion), venceEn);
        var creada = await repository.CrearSesionAsync(sesion, ahora, ct);
        sesionSalida.Presentar(creada
            ? Resultado<SesionAbierta>.Exito(new(UsuarioSesionDto.Desde(usuario), idSesion, emitidaEn, venceEn))
            : Resultado<SesionAbierta>.Fallo(CredencialesInvalidas));
    }

    public async Task InvitarAsync(int id, TipoInvitacion tipo, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            invitacionSalida.Presentar(Resultado<InvitacionActivacion>.Fallo(error));
            return;
        }

        var token = TokensAcceso.Crear();
        var venceEn = reloj.GetUtcNow().AddHours(24);
        var estado = await repository.GuardarInvitacionAsync(id, tipo, TokensAcceso.Huella(token), venceEn, ct);
        invitacionSalida.Presentar(estado == EstadoCuenta.Guardado
            ? Resultado<InvitacionActivacion>.Exito(new(token, venceEn))
            : Resultado<InvitacionActivacion>.Fallo(ErrorDeCuenta(estado)));
    }

    public async Task ActivarAsync(DatosActivacion datos, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        if (!TokensAcceso.EsValido(datos.Token))
        {
            errores["token"] = ["Ingresá el código de activación que recibiste."];
        }

        ContrasenaEspecificacion.ValidarNueva(datos.Contrasena, errores);
        if (errores.Count > 0)
        {
            activacionSalida.Presentar(Resultado<bool>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var hash = contrasenas.CrearHash(datos.Contrasena!);
        var estado = await repository.ActivarAsync(TokensAcceso.Huella(datos.Token!), hash, reloj.GetUtcNow(), ct);
        activacionSalida.Presentar(estado == EstadoCuenta.Guardado
            ? Resultado<bool>.Exito(true)
            : Resultado<bool>.Fallo(ErrorDeCuenta(estado)));
    }

    public async Task MiMembresiaAsync(int clienteId, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zonaHoraria).DateTime);
        var consulta = await repository.BuscarMembresiaPropiaAsync(clienteId, hoy, ct);
        if (consulta is null)
        {
            membresiaSalida.Presentar(Resultado<MiMembresiaDto>.Fallo(
                new(TipoError.Autenticacion, "autenticacion_requerida", "Iniciá sesión para continuar.")));
            return;
        }

        var periodo = consulta.Membresia is { } membresia ? MembresiaDto.Desde(membresia, hoy) : null;
        membresiaSalida.Presentar(Resultado<MiMembresiaDto>.Exito(
            new(ClienteDto.Desde(consulta.Cliente), periodo, consulta.NombrePlan)));
    }

    public async Task MisPagosAsync(int clienteId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var offset = ValidacionEntrada.CalcularOffset(pagina, tamanoPagina);
        if (offset.Error is { } error)
        {
            pagosSalida.Presentar(Resultado<PaginaPagosCuenta>.Fallo(error));
            return;
        }

        var pagos = await repository.BuscarPagosPropiosAsync(clienteId, offset.Valor, tamanoPagina + 1, ct);
        pagosSalida.Presentar(Resultado<PaginaPagosCuenta>.Exito(
            new(pagos.Take(tamanoPagina).ToArray(), pagina, tamanoPagina, pagos.Count > tamanoPagina)));
    }

    private static ErrorAplicacion ErrorDeCuenta(EstadoCuenta estado) => estado switch
    {
        EstadoCuenta.ClienteInexistente => new(TipoError.NoEncontrado, "cliente_no_encontrado", "No se encontró el cliente."),
        EstadoCuenta.ClienteInactivo => new(TipoError.Conflicto, "cliente_inactivo", "El cliente está inactivo."),
        EstadoCuenta.AccesoYaHabilitado => new(TipoError.Conflicto, "acceso_ya_habilitado", "La cuenta ya tiene acceso web."),
        EstadoCuenta.RecepcionistaInexistente => new(TipoError.NoEncontrado, "recepcionista_no_encontrado", "No se encontró el recepcionista."),
        EstadoCuenta.RecepcionistaInactivo => new(TipoError.Conflicto, "recepcionista_inactivo", "El recepcionista está inactivo."),
        EstadoCuenta.EmailOcupado => new(TipoError.Conflicto, "email_acceso_ocupado",
            "El correo ya está asociado al acceso de otra cuenta."),
        EstadoCuenta.ActivacionInvalida => new(TipoError.Validacion, "activacion_invalida", "El código no está disponible. Pedí una nueva invitación."),
        _ => throw new ArgumentOutOfRangeException(nameof(estado))
    };
}

using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class RecepcionistasInteractor(IRecepcionistaRepository repository, IPresentador<RecepcionistaDto> ficha,
    IPresentador<PaginaRecepcionistas> listado, IPresentador<Guid> baja) : IRecepcionistasEntrada
{
    private static readonly ErrorAplicacion NoEncontrado =
        new(TipoError.NoEncontrado, "recepcionista_no_encontrado", "No se encontró el recepcionista.");

    public async Task RegistrarAsync(DatosRecepcionista datos, CancellationToken ct)
    {
        var validado = Validar(datos);
        if (validado.Error is { } error)
        {
            ficha.Presentar(Resultado<RecepcionistaDto>.Fallo(error));
            return;
        }

        var (nombre, apellido, email) = validado.Valor;
        var recepcionista = new Recepcionista(nombre, apellido, email);
        repository.Agregar(recepcionista);
        await GuardarFichaAsync(recepcionista, ct);
    }

    public async Task ObtenerAsync(int id, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            ficha.Presentar(Resultado<RecepcionistaDto>.Fallo(error));
            return;
        }

        var recepcionista = await repository.BuscarAsync(id, ct);
        ficha.Presentar(recepcionista is not null
            ? Resultado<RecepcionistaDto>.Exito(RecepcionistaDto.Desde(recepcionista))
            : Resultado<RecepcionistaDto>.Fallo(NoEncontrado));
    }

    public async Task ListarAsync(bool? activo, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var offset = ValidacionEntrada.CalcularOffset(pagina, tamanoPagina);
        if (offset.Error is { } error)
        {
            listado.Presentar(Resultado<PaginaRecepcionistas>.Fallo(error));
            return;
        }

        var registros = await repository.ListarAsync(activo, offset.Valor, tamanoPagina + 1, ct);
        listado.Presentar(Resultado<PaginaRecepcionistas>.Exito(
            new(registros.Take(tamanoPagina).Select(RecepcionistaDto.Desde).ToArray(), pagina, tamanoPagina,
                registros.Count > tamanoPagina)));
    }

    public async Task ModificarAsync(int id, DatosRecepcionista datos, Guid? version, CancellationToken ct)
    {
        var validado = Validar(datos);
        if ((ValidacionEntrada.ValidarId(id) ?? validado.Error) is { } error)
        {
            ficha.Presentar(Resultado<RecepcionistaDto>.Fallo(error));
            return;
        }

        var recepcionista = await repository.BuscarAsync(id, ct);
        if (recepcionista is not { Activo: true })
        {
            ficha.Presentar(Resultado<RecepcionistaDto>.Fallo(recepcionista is null
                ? NoEncontrado
                : new(TipoError.Conflicto, "recepcionista_inactivo", "El recepcionista está inactivo.")));
            return;
        }

        if (version is { } esperada && recepcionista.Version != esperada)
        {
            ficha.Presentar(Resultado<RecepcionistaDto>.Fallo(ValidacionEntrada.VersionDesactualizada));
            return;
        }

        var (nombre, apellido, email) = validado.Valor;
        recepcionista.Actualizar(nombre, apellido, email);
        await GuardarFichaAsync(recepcionista, ct, esEdicion: true);
    }

    public async Task DesactivarAsync(int id, Guid? version, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            baja.Presentar(Resultado<Guid>.Fallo(error));
            return;
        }

        var recepcionista = await repository.BuscarAsync(id, ct);
        if (recepcionista is null)
        {
            baja.Presentar(Resultado<Guid>.Fallo(NoEncontrado));
            return;
        }

        if (version is { } esperada && recepcionista.Version != esperada)
        {
            baja.Presentar(Resultado<Guid>.Fallo(ValidacionEntrada.VersionDesactualizada));
            return;
        }

        if (recepcionista.Activo)
        {
            recepcionista.Desactivar();
            var estado = await repository.GuardarAsync(ct);
            var errorGuardado = estado == EstadoGuardadoRecepcionista.CambioConcurrente
                ? ValidacionEntrada.VersionDesactualizada
                : TraducirGuardado(estado);
            if (errorGuardado is { } conflicto)
            {
                baja.Presentar(Resultado<Guid>.Fallo(conflicto));
                return;
            }
        }

        baja.Presentar(Resultado<Guid>.Exito(recepcionista.Version));
    }

    private async Task GuardarFichaAsync(Recepcionista recepcionista, CancellationToken ct, bool esEdicion = false)
    {
        var estado = await repository.GuardarAsync(ct);
        var error = esEdicion && estado == EstadoGuardadoRecepcionista.CambioConcurrente
            ? ValidacionEntrada.VersionDesactualizada
            : TraducirGuardado(estado);
        ficha.Presentar(error is null
            ? Resultado<RecepcionistaDto>.Exito(RecepcionistaDto.Desde(recepcionista))
            : Resultado<RecepcionistaDto>.Fallo(error));
    }

    private static Resultado<(string Nombre, string Apellido, string Email)> Validar(DatosRecepcionista datos)
    {
        var errores = new Dictionary<string, string[]>();
        var nombre = ValidacionEntrada.Texto(datos.Nombre, "nombre", "El nombre", LimitesCliente.Nombre, errores);
        var apellido = ValidacionEntrada.Texto(datos.Apellido, "apellido", "El apellido", LimitesCliente.Apellido, errores);
        var email = ClienteEspecificacion.ValidarEmail(datos.Email, errores);
        return errores.Count == 0
            ? Resultado<(string, string, string)>.Exito((nombre, apellido, email))
            : Resultado<(string, string, string)>.Fallo(ValidacionEntrada.Error(errores));
    }

    private static ErrorAplicacion? TraducirGuardado(EstadoGuardadoRecepcionista estado) => estado switch
    {
        EstadoGuardadoRecepcionista.Guardado => null,
        EstadoGuardadoRecepcionista.EmailOcupado => new(TipoError.Conflicto, "email_acceso_ocupado",
            "El correo ya está asociado al acceso de otra cuenta.", new() { ["email"] = ["Elegí otro correo."] }),
        EstadoGuardadoRecepcionista.CambioConcurrente => new(TipoError.Conflicto, "recepcionista_modificado",
            "La ficha cambió durante la operación. Volvé a consultar sus datos."),
        _ => throw new ArgumentOutOfRangeException(nameof(estado))
    };
}

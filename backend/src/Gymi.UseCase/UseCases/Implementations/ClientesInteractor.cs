using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class ClientesInteractor(
    IClienteRepository repository,
    IPresentador<ClienteDto> clienteSalida,
    IPresentador<PaginaClientes> paginaSalida,
    IPresentador<Guid> bajaSalida,
    TimeProvider reloj,
    TimeZoneInfo zonaHoraria) : IClientesEntrada
{
    private static readonly ErrorAplicacion NoEncontrado =
        new(TipoError.NoEncontrado, "cliente_no_encontrado", "No se encontró el cliente.");

    private static readonly ErrorAplicacion DniDuplicado =
        new(TipoError.Conflicto, "dni_duplicado", "Ya existe un cliente con ese DNI.",
            new() { ["dni"] = ["Ya existe un cliente con ese DNI."] });

    public async Task RegistrarAsync(DatosCliente datos, CancellationToken ct)
    {
        var validacion = ClienteEspecificacion.Validar(datos);
        if (validacion.Error is { } error)
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(error));
            return;
        }

        var ficha = validacion.Valor!;
        if (await repository.ExisteDniAsync(ficha.Dni, null, ct))
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(DniDuplicado));
            return;
        }

        var fechaAlta = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zonaHoraria).Date);
        var cliente = new Cliente(ficha.Nombre, ficha.Apellido, ficha.Email, ficha.Dni, ficha.Telefono, fechaAlta);
        repository.Agregar(cliente);
        await GuardarClienteAsync(cliente, ct);
    }

    public async Task ObtenerAsync(int id, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(error));
            return;
        }

        var cliente = await repository.BuscarAsync(id, ct);
        clienteSalida.Presentar(cliente is { Activo: true }
            ? Resultado<ClienteDto>.Exito(ClienteDto.Desde(cliente))
            : Resultado<ClienteDto>.Fallo(NoEncontrado));
    }

    public async Task ListarAsync(int pagina, int tamanoPagina, string? busqueda, CancellationToken ct)
    {
        var offset = ValidacionEntrada.CalcularOffset(pagina, tamanoPagina);
        if (offset.Error is { } error)
        {
            paginaSalida.Presentar(Resultado<PaginaClientes>.Fallo(error));
            return;
        }

        var filtro = ClienteEspecificacion.ValidarBusqueda(busqueda);
        if (filtro.Error is { } errorBusqueda)
        {
            paginaSalida.Presentar(Resultado<PaginaClientes>.Fallo(errorBusqueda));
            return;
        }

        var registros = await repository.ListarActivosAsync(offset.Valor, tamanoPagina + 1, filtro.Valor, ct);
        var clientes = registros.Take(tamanoPagina).Select(ClienteDto.Desde).ToArray();
        paginaSalida.Presentar(Resultado<PaginaClientes>.Exito(
            new(clientes, pagina, tamanoPagina, registros.Count > tamanoPagina)));
    }

    public async Task ModificarAsync(int id, DatosCliente datos, Guid? version, CancellationToken ct)
    {
        var validacion = ClienteEspecificacion.Validar(datos);
        var error = ValidacionEntrada.ValidarId(id) ?? validacion.Error;
        if (error is not null)
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(error));
            return;
        }

        var cliente = await repository.BuscarAsync(id, ct);
        if (cliente is not { Activo: true })
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(NoEncontrado));
            return;
        }

        if (version is { } esperada && cliente.Version != esperada)
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(ValidacionEntrada.VersionDesactualizada));
            return;
        }

        var ficha = validacion.Valor!;
        if (await repository.ExisteDniAsync(ficha.Dni, id, ct))
        {
            clienteSalida.Presentar(Resultado<ClienteDto>.Fallo(DniDuplicado));
            return;
        }

        cliente.Actualizar(ficha.Nombre, ficha.Apellido, ficha.Email, ficha.Dni, ficha.Telefono);
        await GuardarClienteAsync(cliente, ct, esEdicion: true);
    }

    public async Task DesactivarAsync(int id, Guid? version, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            bajaSalida.Presentar(Resultado<Guid>.Fallo(error));
            return;
        }

        var cliente = await repository.BuscarAsync(id, ct);
        if (cliente is null)
        {
            bajaSalida.Presentar(Resultado<Guid>.Fallo(NoEncontrado));
            return;
        }

        if (version is { } esperada && cliente.Version != esperada)
        {
            bajaSalida.Presentar(Resultado<Guid>.Fallo(ValidacionEntrada.VersionDesactualizada));
            return;
        }

        if (!cliente.Activo)
        {
            bajaSalida.Presentar(Resultado<Guid>.Exito(cliente.Version));
            return;
        }

        cliente.Desactivar();
        var estado = await repository.GuardarAsync(ct);
        var errorGuardado = estado == EstadoGuardadoCliente.CambioConcurrente
            ? ValidacionEntrada.VersionDesactualizada
            : TraducirGuardado(estado);
        bajaSalida.Presentar(errorGuardado is null
            ? Resultado<Guid>.Exito(cliente.Version)
            : Resultado<Guid>.Fallo(errorGuardado));
    }

    private async Task GuardarClienteAsync(Cliente cliente, CancellationToken ct, bool esEdicion = false)
    {
        var estado = await repository.GuardarAsync(ct);
        var error = esEdicion && estado == EstadoGuardadoCliente.CambioConcurrente
            ? ValidacionEntrada.VersionDesactualizada
            : TraducirGuardado(estado);
        clienteSalida.Presentar(error is null
            ? Resultado<ClienteDto>.Exito(ClienteDto.Desde(cliente))
            : Resultado<ClienteDto>.Fallo(error));
    }

    private static ErrorAplicacion? TraducirGuardado(EstadoGuardadoCliente estado) => estado switch
    {
        EstadoGuardadoCliente.Guardado => null,
        EstadoGuardadoCliente.DniDuplicado => DniDuplicado,
        EstadoGuardadoCliente.EmailAccesoDuplicado => new(TipoError.Conflicto, "email_acceso_ocupado",
            "El correo ya está asociado al acceso de otra cuenta.", new() { ["email"] = ["Elegí otro correo."] }),
        EstadoGuardadoCliente.CambioConcurrente => new(TipoError.Conflicto, "cliente_modificado",
            "El cliente cambió mientras procesábamos la solicitud. Volvé a consultar sus datos."),
        _ => throw new ArgumentOutOfRangeException(nameof(estado))
    };
}

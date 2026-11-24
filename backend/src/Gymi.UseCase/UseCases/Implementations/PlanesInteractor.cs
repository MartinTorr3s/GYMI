using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class PlanesInteractor(
    IPlanRepository repository,
    IPresentador<PlanDto> planSalida,
    IPresentador<VistaPreviaPlanMensual> vistaPreviaSalida,
    IPresentador<PaginaPlanes> paginaSalida,
    IPresentador<Guid> bajaSalida) : IPlanesEntrada
{
    private static readonly ErrorAplicacion NoEncontrado =
        new(TipoError.NoEncontrado, "plan_no_encontrado", "No se encontró el plan.");

    public void PrevisualizarMensual(DatosPrevisualizacionPlanMensual datos) =>
        vistaPreviaSalida.Presentar(PlanEspecificacion.Previsualizar(datos.SesionesPorSemana));

    public async Task RegistrarAsync(DatosPlan datos, CancellationToken ct)
    {
        var validacion = PlanEspecificacion.Validar(datos);
        if (validacion.Error is { } error)
        {
            planSalida.Presentar(Resultado<PlanDto>.Fallo(error));
            return;
        }

        var ficha = validacion.Valor!;
        var plan = new PlanMembresia(ficha.Nombre, ficha.Descripcion, ficha.Precio,
            ficha.DuracionDias, ficha.CantidadSesiones, ficha.Activo);
        repository.Agregar(plan);
        await repository.GuardarNuevoAsync(ct);
        planSalida.Presentar(Resultado<PlanDto>.Exito(PlanDto.Desde(plan)));
    }

    public async Task ObtenerAsync(int id, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            planSalida.Presentar(Resultado<PlanDto>.Fallo(error));
            return;
        }

        var plan = await repository.BuscarAsync(id, ct);
        planSalida.Presentar(plan is null
            ? Resultado<PlanDto>.Fallo(NoEncontrado)
            : Resultado<PlanDto>.Exito(PlanDto.Desde(plan)));
    }

    public async Task ListarAsync(int pagina, int tamanoPagina, bool? activo, CancellationToken ct)
    {
        var offset = ValidacionEntrada.CalcularOffset(pagina, tamanoPagina);
        if (offset.Error is { } error)
        {
            paginaSalida.Presentar(Resultado<PaginaPlanes>.Fallo(error));
            return;
        }

        var registros = await repository.ListarAsync(offset.Valor, tamanoPagina + 1, activo, ct);
        var planes = registros.Take(tamanoPagina).Select(PlanDto.Desde).ToArray();
        paginaSalida.Presentar(Resultado<PaginaPlanes>.Exito(
            new(planes, pagina, tamanoPagina, registros.Count > tamanoPagina)));
    }

    public async Task ModificarAsync(int id, DatosPlan datos, Guid? version, CancellationToken ct)
    {
        var validacion = PlanEspecificacion.Validar(datos);
        var error = ValidacionEntrada.ValidarId(id) ?? validacion.Error;
        if (error is not null)
        {
            planSalida.Presentar(Resultado<PlanDto>.Fallo(error));
            return;
        }

        var plan = await repository.BuscarAsync(id, ct);
        if (plan is null)
        {
            planSalida.Presentar(Resultado<PlanDto>.Fallo(NoEncontrado));
            return;
        }

        if (version is { } esperada && plan.Version != esperada)
        {
            planSalida.Presentar(Resultado<PlanDto>.Fallo(ValidacionEntrada.VersionDesactualizada));
            return;
        }

        var ficha = validacion.Valor!;
        plan.Actualizar(ficha.Nombre, ficha.Descripcion, ficha.Precio, ficha.DuracionDias,
            ficha.CantidadSesiones, ficha.Activo);
        planSalida.Presentar(await repository.IntentarGuardarCambioAsync(ct)
            ? Resultado<PlanDto>.Exito(PlanDto.Desde(plan))
            : Resultado<PlanDto>.Fallo(ValidacionEntrada.VersionDesactualizada));
    }

    public async Task DesactivarAsync(int id, Guid? version, CancellationToken ct)
    {
        if (ValidacionEntrada.ValidarId(id) is { } error)
        {
            bajaSalida.Presentar(Resultado<Guid>.Fallo(error));
            return;
        }

        var plan = await repository.BuscarAsync(id, ct);
        if (plan is null)
        {
            bajaSalida.Presentar(Resultado<Guid>.Fallo(NoEncontrado));
            return;
        }

        if (version is { } esperada && plan.Version != esperada)
        {
            bajaSalida.Presentar(Resultado<Guid>.Fallo(ValidacionEntrada.VersionDesactualizada));
            return;
        }

        if (plan.Activo)
        {
            plan.Desactivar();
            if (!await repository.IntentarGuardarCambioAsync(ct))
            {
                bajaSalida.Presentar(Resultado<Guid>.Fallo(ValidacionEntrada.VersionDesactualizada));
                return;
            }
        }

        bajaSalida.Presentar(Resultado<Guid>.Exito(plan.Version));
    }
}

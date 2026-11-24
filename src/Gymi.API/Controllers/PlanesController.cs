using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class PlanesController(
    IPlanesEntrada entrada,
    IPresentador<PlanDto> plan,
    IPresentador<VistaPreviaPlanMensual> vistaPrevia,
    IPresentador<PaginaPlanes> pagina,
    IPresentador<Guid> baja) : IPlanesController
{
    public Resultado<VistaPreviaPlanMensual> PrevisualizarMensual(DatosPrevisualizacionPlanMensual datos)
    {
        entrada.PrevisualizarMensual(datos);
        return vistaPrevia.Resultado;
    }

    public async Task<Resultado<PlanDto>> RegistrarAsync(DatosPlan datos, CancellationToken ct)
    {
        await entrada.RegistrarAsync(datos, ct);
        return plan.Resultado;
    }

    public async Task<Resultado<PlanDto>> ObtenerAsync(int id, CancellationToken ct)
    {
        await entrada.ObtenerAsync(id, ct);
        return plan.Resultado;
    }

    public async Task<Resultado<PaginaPlanes>> ListarAsync(int numero, int tamano, bool? activo, CancellationToken ct)
    {
        await entrada.ListarAsync(numero, tamano, activo, ct);
        return pagina.Resultado;
    }

    public async Task<Resultado<PlanDto>> ModificarAsync(int id, DatosPlan datos, Guid? version, CancellationToken ct)
    {
        await entrada.ModificarAsync(id, datos, version, ct);
        return plan.Resultado;
    }

    public async Task<Resultado<Guid>> DesactivarAsync(int id, Guid? version, CancellationToken ct)
    {
        await entrada.DesactivarAsync(id, version, ct);
        return baja.Resultado;
    }
}

public interface IPlanesController
{
    Resultado<VistaPreviaPlanMensual> PrevisualizarMensual(DatosPrevisualizacionPlanMensual datos);
    Task<Resultado<PlanDto>> RegistrarAsync(DatosPlan datos, CancellationToken ct);
    Task<Resultado<PlanDto>> ObtenerAsync(int id, CancellationToken ct);
    Task<Resultado<PaginaPlanes>> ListarAsync(int pagina, int tamanoPagina, bool? activo, CancellationToken ct);
    Task<Resultado<PlanDto>> ModificarAsync(int id, DatosPlan datos, Guid? version, CancellationToken ct);
    Task<Resultado<Guid>> DesactivarAsync(int id, Guid? version, CancellationToken ct);
}

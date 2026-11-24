using Gymi.Entity;

namespace Gymi.UseCase;

public interface IPlanesEntrada
{
    void PrevisualizarMensual(DatosPrevisualizacionPlanMensual datos);
    Task RegistrarAsync(DatosPlan datos, CancellationToken ct);
    Task ObtenerAsync(int id, CancellationToken ct);
    Task ListarAsync(int pagina, int tamanoPagina, bool? activo, CancellationToken ct);
    Task ModificarAsync(int id, DatosPlan datos, Guid? version, CancellationToken ct);
    Task DesactivarAsync(int id, Guid? version, CancellationToken ct);
}

public interface IPlanRepository
{
    Task<PlanMembresia?> BuscarAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<PlanMembresia>> ListarAsync(int offset, int cantidad, bool? activo, CancellationToken ct);
    void Agregar(PlanMembresia plan);
    Task GuardarNuevoAsync(CancellationToken ct);
    Task<bool> IntentarGuardarCambioAsync(CancellationToken ct);
}

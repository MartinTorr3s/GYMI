using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class PlanRepository(GymiDbContext db) : IPlanRepository
{
    public Task<PlanMembresia?> BuscarAsync(int id, CancellationToken ct) =>
        db.Planes.SingleOrDefaultAsync(plan => plan.Id == id, ct);

    public async Task<IReadOnlyList<PlanMembresia>> ListarAsync(int offset, int cantidad, bool? activo, CancellationToken ct)
    {
        var consulta = db.Planes.AsNoTracking();
        if (activo is { } estado)
        {
            consulta = consulta.Where(plan => plan.Activo == estado);
        }

        return await consulta.OrderBy(plan => plan.Id).Skip(offset).Take(cantidad).ToListAsync(ct);
    }

    public void Agregar(PlanMembresia plan) => db.Planes.Add(plan);

    public async Task GuardarNuevoAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);

    public async Task<bool> IntentarGuardarCambioAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}

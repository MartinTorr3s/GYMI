using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class RecepcionistaRepository(GymiDbContext db) : IRecepcionistaRepository
{
    public void Agregar(Recepcionista recepcionista) => db.Set<Recepcionista>().Add(recepcionista);

    public Task<Recepcionista?> BuscarAsync(int id, CancellationToken ct) =>
        db.Set<Recepcionista>().SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Recepcionista>> ListarAsync(bool? activo, int offset, int cantidad, CancellationToken ct)
    {
        var recepcionistas = db.Set<Recepcionista>().AsNoTracking();
        if (activo is { } estado)
        {
            recepcionistas = recepcionistas.Where(r => r.Activo == estado);
        }

        return await recepcionistas.OrderBy(r => r.Id).Skip(offset).Take(cantidad).ToListAsync(ct);
    }

    public async Task<EstadoGuardadoRecepcionista> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return EstadoGuardadoRecepcionista.Guardado;
        }
        catch (DbUpdateConcurrencyException)
        {
            return EstadoGuardadoRecepcionista.CambioConcurrente;
        }
        catch (DbUpdateException ex) when (ConflictosSqlite.EsEmailDeAccesoOcupado(ex))
        {
            return EstadoGuardadoRecepcionista.EmailOcupado;
        }
    }
}

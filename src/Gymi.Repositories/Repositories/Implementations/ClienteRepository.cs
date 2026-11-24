using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class ClienteRepository(GymiDbContext db) : IClienteRepository
{
    public Task<Cliente?> BuscarAsync(int id, CancellationToken ct) =>
        db.Clientes.SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Cliente>> ListarActivosAsync(int offset, int cantidad, string? busqueda,
        CancellationToken ct)
    {
        var consulta = db.Clientes.AsNoTracking().Where(c => c.Activo);
        if (busqueda is not null)
        {
            consulta = busqueda.All(char.IsAsciiDigit)
                ? consulta.Where(c => c.Dni == busqueda)
                : consulta.Where(c => FuncionesSqlite.ContieneSinTildes(c.Nombre + " " + c.Apellido, busqueda));
        }

        return await consulta.OrderBy(c => c.Id).Skip(offset).Take(cantidad).ToListAsync(ct);
    }

    public Task<bool> ExisteDniAsync(string dni, int? idExceptuado, CancellationToken ct) =>
        db.Clientes.AnyAsync(c => c.Dni == dni && c.Id != idExceptuado, ct);

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);

    public async Task<EstadoGuardadoCliente> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return EstadoGuardadoCliente.Guardado;
        }
        catch (DbUpdateConcurrencyException)
        {
            return EstadoGuardadoCliente.CambioConcurrente;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqlite &&
            sqlite.SqliteExtendedErrorCode == 2067 &&
            sqlite.Message.Contains("UNIQUE constraint failed: Clientes.Dni'", StringComparison.Ordinal))
        {
            return EstadoGuardadoCliente.DniDuplicado;
        }
        catch (DbUpdateException ex) when (ConflictosSqlite.EsEmailDeAccesoOcupado(ex))
        {
            return EstadoGuardadoCliente.EmailAccesoDuplicado;
        }
    }
}

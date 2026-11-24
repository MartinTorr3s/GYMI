using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

internal static class TransaccionSqlite
{
    public static async Task<T> ConEscrituraReservadaAsync<T>(this GymiDbContext db, Func<Task<T>> ejecutar, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            // SQLite reserva la escritura antes de consultar el estado que se va a modificar.
            var conexion = (SqliteConnection)db.Database.GetDbConnection();
            await using var transaccion = conexion.BeginTransaction(deferred: false);
            await using var operacion = await db.Database.UseTransactionAsync(transaccion, ct);
            var resultado = await ejecutar();
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}

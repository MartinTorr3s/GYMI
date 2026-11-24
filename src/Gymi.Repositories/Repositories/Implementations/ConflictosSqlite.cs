using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

internal static class ConflictosSqlite
{
    public static bool EsEmailDeAccesoOcupado(DbUpdateException excepcion) =>
        excepcion.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 } sqlite &&
        sqlite.Message.Contains("UNIQUE constraint failed: Usuarios.EmailAccesoNormalizado'", StringComparison.Ordinal);
}

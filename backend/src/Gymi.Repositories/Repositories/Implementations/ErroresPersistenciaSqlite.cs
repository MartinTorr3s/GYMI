using Gymi.UseCase;
using Microsoft.Data.Sqlite;

namespace Gymi.Repositories;

public sealed class ErroresPersistenciaSqlite : IErroresPersistencia
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    public bool EsContencionTemporal(Exception excepcion)
    {
        for (Exception? actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqliteException { SqliteErrorCode: SqliteBusy or SqliteLocked })
            {
                return true;
            }
        }

        return false;
    }
}

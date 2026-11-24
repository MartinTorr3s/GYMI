using System.Data.Common;
using System.Globalization;
using Gymi.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gymi.Repositories;

internal sealed class FuncionesSqlite : DbConnectionInterceptor
{
    public const string NombreContieneSinTildes = "gymi_contiene_sin_tildes";
    public static readonly FuncionesSqlite Instancia = new();

    public static bool ContieneSinTildes(string texto, string termino) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(texto, termino,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var sqlite = (SqliteConnection)connection;
        sqlite.CreateFunction<string, string, bool>(NombreContieneSinTildes, ContieneSinTildes, isDeterministic: true);
        sqlite.CreateFunction<string, string>("gymi_email_acceso", Usuario.NormalizarEmail, isDeterministic: true);
        sqlite.CreateFunction<string, string>("gymi_email_legado", email => email.Trim().ToUpperInvariant(),
            isDeterministic: true);
    }

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ConnectionOpened(connection, eventData);
        return Task.CompletedTask;
    }
}

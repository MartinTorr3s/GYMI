using Gymi.UseCase;
using Gymi.Repositories.Cache;
using Gymi.Repositories;
using Gymi.Repositories.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gymi.Repositories;

public static class ServiciosInfraestructura
{
    public static IServiceCollection AgregarInfraestructuraGymi(this IServiceCollection servicios, string conexion)
    {
        var opcionesSqlite = new SqliteConnectionStringBuilder(conexion);
        if (!opcionesSqlite.ShouldSerialize("Default Timeout"))
        {
            opcionesSqlite.DefaultTimeout = 2;
        }

        servicios.AddDbContext<GymiDbContext>(opciones => opciones.UseSqlite(opcionesSqlite.ConnectionString));
        servicios.AddSingleton<IErroresPersistencia, ErroresPersistenciaSqlite>();
        servicios.AddScoped<IClienteRepository, ClienteRepository>();
        servicios.AddScoped<IPlanRepository, PlanRepository>();
        servicios.AddScoped<IMembresiaRepository, MembresiaRepository>();
        servicios.AddScoped<IPagoRepository, PagoRepository>();
        servicios.AddScoped<IAsistenciaRepository, AsistenciaRepository>();
        servicios.AddSingleton<IProteccionContrasenas, ProteccionContrasenas>();
        servicios.AddScoped<IAccesoRepository, AccesoRepository>();
        servicios.AddScoped<IRecepcionistaRepository, RecepcionistaRepository>();
        servicios.AddSingleton<ICursoresReporte, CursoresReporte>();
        servicios.AddScoped<IReporteRepository, ReporteRepository>();

        return servicios;
    }

    public static async Task<bool> InicializarBaseLocalAsync(this IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GymiDbContext>();
        return await db.MigrarBaseLocalAsync();
    }
}

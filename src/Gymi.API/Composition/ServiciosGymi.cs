using Gymi.UseCase;
using Gymi.UseCase.UseCases.Implementations;
using Gymi.Repositories;
using Gymi.API.Controllers;
using Gymi.API.Presenters;

namespace Gymi.API;

internal static class ServiciosGymi
{
    public static IServiceCollection AgregarGymi(this IServiceCollection servicios, string conexion)
    {
        servicios.AgregarInfraestructuraGymi(conexion);
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"));
        servicios.AddScoped(typeof(IPresentador<>), typeof(Presentador<>));
        servicios.AddScoped<IClientesEntrada, ClientesInteractor>();
        servicios.AddScoped<IClientesController, ClientesController>();
        servicios.AddScoped<IPlanesEntrada, PlanesInteractor>();
        servicios.AddScoped<IPlanesController, PlanesController>();
        servicios.AddScoped<IMembresiasEntrada, MembresiasInteractor>();
        servicios.AddScoped<IMembresiasController, MembresiasController>();
        servicios.AddScoped<IPagosEntrada, PagosInteractor>();
        servicios.AddScoped<IPagosController, PagosController>();
        servicios.AddScoped<IAsistenciasEntrada, AsistenciasInteractor>();
        servicios.AddScoped<IAsistenciasController, AsistenciasController>();
        servicios.AddScoped<IRecepcionistasEntrada, RecepcionistasInteractor>();
        servicios.AddScoped<IRecepcionistasController, RecepcionistasController>();
        servicios.AddScoped<IAccesoEntrada, AccesoInteractor>();
        servicios.AddScoped<IAccesoController, AccesoController>();
        servicios.AddScoped<IControlDeSesion, ControlDeSesion>();
        servicios.AddScoped<IReportesEntrada, ReportesInteractor>();
        servicios.AddScoped<IReportesController, ReportesController>();
        servicios.AddScoped<IProvisionAdministrador, ProvisionAdministrador>();

        return servicios;
    }
}

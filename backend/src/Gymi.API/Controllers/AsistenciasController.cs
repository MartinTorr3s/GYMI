using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class AsistenciasController(IAsistenciasEntrada entrada, IPresentador<ConsultaAsistenciaDto> consulta,
    IPresentador<ConfirmacionAsistencia> registro, IPresentador<AsistenciaDto> detalle) : IAsistenciasController
{
    public async Task<Resultado<ConsultaAsistenciaDto>> ConsultarClienteAsync(DatosConsultaAsistencia datos, CancellationToken ct)
    {
        await entrada.ConsultarClienteAsync(datos, ct);
        return consulta.Resultado;
    }

    public async Task<Resultado<ConfirmacionAsistencia>> RegistrarAsync(int clienteId, DatosRegistroAsistencia datos, CancellationToken ct)
    {
        await entrada.RegistrarAsync(clienteId, datos, ct);
        return registro.Resultado;
    }

    public async Task<Resultado<AsistenciaDto>> ObtenerAsync(int clienteId, int asistenciaId, CancellationToken ct)
    {
        await entrada.ObtenerAsync(clienteId, asistenciaId, ct);
        return detalle.Resultado;
    }
}

public interface IAsistenciasController
{
    Task<Resultado<ConsultaAsistenciaDto>> ConsultarClienteAsync(DatosConsultaAsistencia datos, CancellationToken ct);
    Task<Resultado<ConfirmacionAsistencia>> RegistrarAsync(int clienteId, DatosRegistroAsistencia datos, CancellationToken ct);
    Task<Resultado<AsistenciaDto>> ObtenerAsync(int clienteId, int asistenciaId, CancellationToken ct);
}

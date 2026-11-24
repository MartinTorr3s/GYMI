using Gymi.UseCase;

namespace Gymi.API.Controllers;

public sealed class AccesoController(IAccesoEntrada entrada, IPresentador<SesionAbierta> sesion,
    IPresentador<InvitacionActivacion> invitacion, IPresentador<bool> activacion,
    IPresentador<MiMembresiaDto> membresia, IPresentador<PaginaPagosCuenta> pagos) : IAccesoController
{
    public async Task<Resultado<SesionAbierta>> IniciarSesionAsync(DatosInicioSesion datos, CancellationToken ct)
    {
        await entrada.IniciarSesionAsync(datos, ct);
        return sesion.Resultado;
    }

    public async Task<Resultado<InvitacionActivacion>> InvitarAsync(int id, TipoInvitacion tipo, CancellationToken ct)
    {
        await entrada.InvitarAsync(id, tipo, ct);
        return invitacion.Resultado;
    }

    public async Task<Resultado<bool>> ActivarAsync(DatosActivacion datos, CancellationToken ct)
    {
        await entrada.ActivarAsync(datos, ct);
        return activacion.Resultado;
    }

    public async Task<Resultado<MiMembresiaDto>> MiMembresiaAsync(int clienteId, CancellationToken ct)
    {
        await entrada.MiMembresiaAsync(clienteId, ct);
        return membresia.Resultado;
    }

    public async Task<Resultado<PaginaPagosCuenta>> MisPagosAsync(int clienteId, int pagina, int tamanoPagina,
        CancellationToken ct)
    {
        await entrada.MisPagosAsync(clienteId, pagina, tamanoPagina, ct);
        return pagos.Resultado;
    }
}

public interface IAccesoController
{
    Task<Resultado<SesionAbierta>> IniciarSesionAsync(DatosInicioSesion datos, CancellationToken ct);
    Task<Resultado<InvitacionActivacion>> InvitarAsync(int id, TipoInvitacion tipo, CancellationToken ct);
    Task<Resultado<bool>> ActivarAsync(DatosActivacion datos, CancellationToken ct);
    Task<Resultado<MiMembresiaDto>> MiMembresiaAsync(int clienteId, CancellationToken ct);
    Task<Resultado<PaginaPagosCuenta>> MisPagosAsync(int clienteId, int pagina, int tamanoPagina, CancellationToken ct);
}

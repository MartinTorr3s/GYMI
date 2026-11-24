using Gymi.Entity;

namespace Gymi.UseCase;

public interface IAsistenciasEntrada
{
    Task ConsultarClienteAsync(DatosConsultaAsistencia datos, CancellationToken ct);
    Task RegistrarAsync(int clienteId, DatosRegistroAsistencia datos, CancellationToken ct);
    Task ObtenerAsync(int clienteId, int asistenciaId, CancellationToken ct);
}

public interface IAsistenciaRepository
{
    Task<ConsultaAcceso?> ConsultarClienteAsync(string dni, DateOnly fecha, CancellationToken ct);
    Task<RegistroAsistencia> RegistrarAsync(int clienteId, Guid operacionId, DateTimeOffset fechaHora,
        DateOnly fechaLocal, CancellationToken ct);
    Task<Asistencia?> BuscarAsync(int clienteId, int asistenciaId, CancellationToken ct);
}

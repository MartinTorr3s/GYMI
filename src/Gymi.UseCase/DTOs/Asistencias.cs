using Gymi.Entity;

namespace Gymi.UseCase;

public sealed record DatosConsultaAsistencia(string? Dni);
public sealed record DatosRegistroAsistencia(Guid? OperacionId);

public sealed record ConsultaAsistenciaDto(int ClienteId, string Nombre, string Apellido, MembresiaDto? Membresia,
    bool PuedeIngresar, string? CodigoImpedimento);

public sealed record AsistenciaDto(int Id, int ClienteId, int MembresiaId, Guid OperacionId, DateTimeOffset FechaHora,
    int SesionesRestantes)
{
    public static AsistenciaDto Desde(Asistencia asistencia) =>
        new(asistencia.Id, asistencia.Membresia.ClienteId, asistencia.MembresiaId,
            asistencia.OperacionId, asistencia.FechaHora, asistencia.SesionesRestantes);
}

public sealed record ConfirmacionAsistencia(AsistenciaDto Asistencia, bool EsReintento);

public enum EstadoAcceso
{
    Habilitado,
    ClienteInexistente,
    ClienteInactivo,
    MembresiaNoAsignada,
    MembresiaProgramada,
    MembresiaVencida,
    SesionesAgotadas,
    OperacionAsignada
}

public sealed record ConsultaAcceso(Cliente Cliente, Membresia? Membresia)
{
    public EstadoAcceso Evaluar(DateOnly fecha) => (Cliente.Activo, Membresia?.EstadoEn(fecha)) switch
    {
        (false, _) => EstadoAcceso.ClienteInactivo,
        (_, null) => EstadoAcceso.MembresiaNoAsignada,
        (_, EstadoMembresia.Programada) => EstadoAcceso.MembresiaProgramada,
        (_, EstadoMembresia.Vencida) => EstadoAcceso.MembresiaVencida,
        _ => Membresia!.SesionesDisponibles > 0 ? EstadoAcceso.Habilitado : EstadoAcceso.SesionesAgotadas
    };
}

public sealed record RegistroAsistencia(EstadoAcceso Estado, Asistencia? Asistencia = null, bool EsReintento = false);

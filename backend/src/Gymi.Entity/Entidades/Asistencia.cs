namespace Gymi.Entity;

public sealed class Asistencia
{
    private Asistencia() { }

    internal Asistencia(Membresia membresia, Guid operacionId, DateTimeOffset fechaHora, int sesionesRestantes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(membresia.Id);
        if (operacionId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de operación es obligatorio.", nameof(operacionId));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(sesionesRestantes);

        Membresia = membresia;
        MembresiaId = membresia.Id;
        OperacionId = operacionId;
        FechaHora = fechaHora.ToUniversalTime();
        SesionesRestantes = sesionesRestantes;
    }

    public int Id { get; private set; }
    public int MembresiaId { get; private set; }
    public Membresia Membresia { get; private set; } = null!;
    public Guid OperacionId { get; private set; }
    public DateTimeOffset FechaHora { get; private set; }
    public int SesionesRestantes { get; private set; }
}

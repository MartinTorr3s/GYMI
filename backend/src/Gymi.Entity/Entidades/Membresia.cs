namespace Gymi.Entity;

public sealed class Membresia
{
    private Membresia() { }

    public Membresia(int clienteId, PlanMembresia plan, DateOnly fechaInicio, DateOnly fechaVencimiento,
        int? diaAnclaMensual = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clienteId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plan.Id);
        if (fechaVencimiento <= fechaInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaVencimiento));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(diaAnclaMensual ?? 1, 1, nameof(diaAnclaMensual));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(diaAnclaMensual ?? 31, 31, nameof(diaAnclaMensual));
        DiaAnclaMensual = plan.DuracionDias is null ? diaAnclaMensual ?? fechaInicio.Day : null;
        ClienteId = clienteId;
        PlanId = plan.Id;
        FechaInicio = fechaInicio;
        FechaVencimiento = fechaVencimiento;
        SesionesIncluidas = plan.CantidadSesiones;
        SesionesDisponibles = SesionesIncluidas;
    }

    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public int PlanId { get; private set; }
    public DateOnly FechaInicio { get; private set; }
    public DateOnly FechaVencimiento { get; private set; }
    public int? DiaAnclaMensual { get; private set; }
    public int SesionesIncluidas { get; private set; }
    public int SesionesDisponibles { get; private set; }

    public static DateOnly? CalcularVencimiento(DateOnly inicio, PlanMembresia plan, int? diaAnclaMensual = null)
    {
        if (plan.DuracionDias is { } dias)
        {
            return dias <= DateOnly.MaxValue.DayNumber - inicio.DayNumber ? inicio.AddDays(dias) : null;
        }

        if (inicio.Year == 9999 && inicio.Month == 12)
        {
            return null;
        }

        var mesSiguiente = inicio.AddMonths(1);
        var dia = Math.Min(diaAnclaMensual ?? inicio.Day, DateTime.DaysInMonth(mesSiguiente.Year, mesSiguiente.Month));
        return new DateOnly(mesSiguiente.Year, mesSiguiente.Month, dia);
    }

    public DateOnly InicioDeRenovacion(DateOnly fechaPago) =>
        fechaPago > FechaVencimiento ? fechaPago : FechaVencimiento;

    public int? DiaAnclaDeRenovacion(PlanMembresia plan, DateOnly inicio) => plan.DuracionDias is not null
        ? null
        : inicio == FechaVencimiento ? DiaAnclaMensual ?? inicio.Day : inicio.Day;

    public Asistencia RegistrarAsistencia(Guid operacionId, DateTimeOffset fechaHora, DateOnly fechaLocal)
    {
        if (EstadoEn(fechaLocal) != EstadoMembresia.Vigente || SesionesDisponibles <= 0)
        {
            throw new InvalidOperationException("La membresía no admite el ingreso.");
        }

        var asistencia = new Asistencia(this, operacionId, fechaHora, SesionesDisponibles - 1);
        SesionesDisponibles = asistencia.SesionesRestantes;
        return asistencia;
    }

    public EstadoMembresia EstadoEn(DateOnly fecha) => fecha < FechaInicio
        ? EstadoMembresia.Programada
        : fecha < FechaVencimiento ? EstadoMembresia.Vigente : EstadoMembresia.Vencida;
}

public enum EstadoMembresia
{
    Programada,
    Vigente,
    Vencida
}

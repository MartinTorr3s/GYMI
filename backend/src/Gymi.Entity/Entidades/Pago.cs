namespace Gymi.Entity;

public sealed class Pago
{
    public const int MedioPagoMaximo = 40;
    public const int ObservacionMaxima = 255;
    public const decimal MontoMaximo = 99_999_999.99m;

    private Pago() { }

    public Pago(Membresia origen, Membresia cubierta, TipoPago tipo, DateOnly fechaPago, decimal monto,
        string medioPago, string? observacion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(origen.Id);
        if (tipo switch
        {
            TipoPago.PrimerPago => origen.Id != cubierta.Id,
            TipoPago.Renovacion => cubierta.Id != 0 || cubierta.FechaInicio != origen.InicioDeRenovacion(fechaPago)
                || cubierta.ClienteId != origen.ClienteId || cubierta.PlanId != origen.PlanId,
            _ => true
        })
        {
            throw new ArgumentException("El pago y el período cubierto no son compatibles.", nameof(cubierta));
        }

        if (!EsMontoValido(monto))
        {
            throw new ArgumentOutOfRangeException(nameof(monto));
        }

        MembresiaOrigenId = origen.Id;
        MembresiaCubierta = cubierta;
        Tipo = tipo;
        FechaPago = fechaPago;
        Monto = monto;
        MedioPago = ValidarTexto(medioPago, MedioPagoMaximo, nameof(medioPago));
        Observacion = observacion is null ? null : ValidarTexto(observacion, ObservacionMaxima, nameof(observacion));
    }

    public int Id { get; private set; }
    public int MembresiaOrigenId { get; private set; }
    public int MembresiaCubiertaId { get; private set; }
    public Membresia MembresiaCubierta { get; private set; } = null!;
    public TipoPago Tipo { get; private set; }
    public DateOnly FechaPago { get; private set; }
    public decimal Monto { get; private set; }
    public string MedioPago { get; private set; } = "";
    public string? Observacion { get; private set; }

    public static bool EsMontoValido(decimal monto) =>
        monto is > 0 and <= MontoMaximo && decimal.Round(monto, 2) == monto;

    private static string ValidarTexto(string texto, int maximo, string campo)
    {
        ArgumentNullException.ThrowIfNull(texto, campo);
        var valor = texto.Trim();
        if (valor.Length == 0 || valor.Length > maximo || valor.Any(char.IsControl))
        {
            throw new ArgumentException("El texto del pago no cumple el formato o la longitud.", campo);
        }

        return valor;
    }
}

public enum TipoPago
{
    PrimerPago = 1,
    Renovacion = 2
}

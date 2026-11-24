namespace Gymi.Entity;

public sealed class PlanMembresia
{
    public const int NombreMaximo = 80;
    public const int DescripcionMaxima = 255;
    public const decimal PrecioMaximo = 99_999_999.99m;

    public static readonly int DuracionMaximaDias = DateOnly.MaxValue.DayNumber - DateOnly.MinValue.DayNumber;

    public const int SemanasDeReferenciaMensual = 4;
    public const int MaximoSesionesPorSemana = int.MaxValue / SemanasDeReferenciaMensual;

    private PlanMembresia() { }

    public PlanMembresia(string nombre, string descripcion, decimal precio, int? duracionDias,
        int cantidadSesiones, bool activo)
    {
        Actualizar(nombre, descripcion, precio, duracionDias, cantidadSesiones, activo);
    }

    public int Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Descripcion { get; private set; } = "";
    public decimal Precio { get; private set; }
    public int? DuracionDias { get; private set; }
    public int CantidadSesiones { get; private set; }
    public bool Activo { get; private set; }
    public Guid Version { get; private set; }

    public static bool EsDuracionValida(int dias) => dias > 0 && dias <= DuracionMaximaDias;

    public static bool EsPrecioValido(decimal precio) =>
        precio is >= 0 and <= PrecioMaximo && decimal.Round(precio, 2) == precio;

    public static int CalcularCupoMensual(int sesionesPorSemana)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sesionesPorSemana);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sesionesPorSemana, MaximoSesionesPorSemana);
        return sesionesPorSemana * SemanasDeReferenciaMensual;
    }

    public void Actualizar(string nombre, string descripcion, decimal precio, int? duracionDias,
        int cantidadSesiones, bool activo)
    {
        nombre = ValidarTexto(nombre, NombreMaximo, nameof(nombre));
        descripcion = ValidarTexto(descripcion, DescripcionMaxima, nameof(descripcion));
        if (!EsPrecioValido(precio))
        {
            throw new ArgumentOutOfRangeException(nameof(precio));
        }

        if (duracionDias is { } dias && !EsDuracionValida(dias))
        {
            throw new ArgumentOutOfRangeException(nameof(duracionDias));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidadSesiones);

        Nombre = nombre;
        Descripcion = descripcion;
        Precio = precio;
        DuracionDias = duracionDias;
        CantidadSesiones = cantidadSesiones;
        Activo = activo;
        Version = Guid.NewGuid();
    }

    public void Desactivar()
    {
        Activo = false;
        Version = Guid.NewGuid();
    }

    private static string ValidarTexto(string texto, int maximo, string campo)
    {
        ArgumentNullException.ThrowIfNull(texto, campo);
        var valor = texto.Trim();
        if (valor.Length == 0 || valor.Length > maximo || valor.Any(char.IsControl))
        {
            throw new ArgumentException("El texto no cumple el formato o la longitud del plan.", campo);
        }

        return valor;
    }
}

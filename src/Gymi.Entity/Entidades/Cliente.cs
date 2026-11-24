namespace Gymi.Entity;

public sealed class Cliente : Usuario
{
    private Cliente() { }

    public Cliente(string nombre, string apellido, string email, string dni, string telefono, DateOnly fechaAlta)
        : base(nombre, apellido, email)
    {
        Dni = dni;
        Telefono = telefono;
        FechaAlta = fechaAlta;
    }

    public string Dni { get; private set; } = "";
    public string Telefono { get; private set; } = "";
    public DateOnly FechaAlta { get; private set; }

    public void Actualizar(string nombre, string apellido, string email, string dni, string telefono)
    {
        ActualizarDatos(nombre, apellido, email);
        Dni = dni;
        Telefono = telefono;
    }
}

namespace Gymi.Entity;

public sealed class Recepcionista : Usuario
{
    private Recepcionista() { }

    public Recepcionista(string nombre, string apellido, string email) : base(nombre, apellido, email)
    {
        ReservarEmailDeAcceso();
    }

    public void Actualizar(string nombre, string apellido, string email)
    {
        ActualizarDatos(nombre, apellido, email);
        ReservarEmailDeAcceso();
    }
}

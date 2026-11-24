using Gymi.Entity;

namespace Gymi.UseCase;

public sealed record DatosRecepcionista(string? Nombre, string? Apellido, string? Email);

public sealed record RecepcionistaDto(int Id, string Nombre, string Apellido, string Email, bool Activo,
    bool AccesoWebHabilitado, Guid Version)
{
    public static RecepcionistaDto Desde(Recepcionista recepcionista) =>
        new(recepcionista.Id, recepcionista.Nombre, recepcionista.Apellido, recepcionista.Email,
            recepcionista.Activo, recepcionista.AccesoWebHabilitado, recepcionista.Version);
}

public sealed record PaginaRecepcionistas(IReadOnlyList<RecepcionistaDto> Recepcionistas, int Pagina, int TamanoPagina, bool HayMas);

public enum EstadoGuardadoRecepcionista
{
    Guardado,
    EmailOcupado,
    CambioConcurrente
}

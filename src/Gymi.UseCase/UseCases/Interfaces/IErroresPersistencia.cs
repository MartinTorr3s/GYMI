namespace Gymi.UseCase;

public interface IErroresPersistencia
{
    bool EsContencionTemporal(Exception excepcion);
}

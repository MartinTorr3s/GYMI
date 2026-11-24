using Gymi.UseCase;
using Microsoft.Extensions.Caching.Memory;

namespace Gymi.Repositories.Cache;

internal sealed class CursoresReporte : ICursoresReporte, IDisposable
{
    private const int Capacidad = 2048;
    private readonly Lock _escritura = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = Capacidad });

    public ContinuacionReporte? Buscar(Guid cursor) =>
        _cache.TryGetValue<ContinuacionReporte>(cursor, out var continuacion) ? continuacion : null;

    public Guid Guardar(ContinuacionReporte continuacion)
    {
        var cursor = Guid.NewGuid();
        lock (_escritura)
        {
            if (_cache.Count >= Capacidad)
            {
                _cache.Compact(0.25);
            }

            _cache.Set(cursor, continuacion, new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpiration = continuacion.FechaGeneracion.AddMinutes(15)
            });
        }

        return cursor;
    }

    public void Dispose() => _cache.Dispose();
}

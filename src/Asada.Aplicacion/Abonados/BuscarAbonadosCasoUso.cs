using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;

namespace Asada.Aplicacion.Abonados;

public class BuscarAbonadosCasoUso(IRepositorioAbonados repositorio)
{
    public const int TamanoPaginaPorDefecto = 20;
    public const int TamanoPaginaMaximo = 100;

    /// <summary>Busca por nombre, identificacion o codigo. Sin texto devuelve todos, paginados.</summary>
    public Task<PaginaResultado<AbonadoResumenDto>> EjecutarAsync(
        string? texto, int pagina = 1, int tamanoPagina = TamanoPaginaPorDefecto, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, TamanoPaginaMaximo);
        return repositorio.BuscarAsync(Validacion.Limpiar(texto), pagina, tamanoPagina, ct);
    }
}

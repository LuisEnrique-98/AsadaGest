using Asada.Aplicacion.Common;
using Asada.Aplicacion.Recibos.Dtos;

namespace Asada.Aplicacion.Recibos;

public class ObtenerReciboCasoUso(IRepositorioRecibos repositorio)
{
    public async Task<Resultado<ReciboDto>> EjecutarAsync(int reciboId, CancellationToken ct = default)
    {
        if (await repositorio.ObtenerReciboAsync(reciboId, ct) is not var (r, periodo))
            return Resultado<ReciboDto>.Falla("El recibo no existe.");

        var org = await repositorio.ObtenerOrganizacionAsync(r.OrganizacionId, ct);
        return Resultado<ReciboDto>.Ok(MapeoRecibos.ADto(r, periodo, org));
    }
}

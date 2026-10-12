using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Abonados;

public class CambiarEstadoAbonadoCasoUso(IRepositorioAbonados repositorio)
{
    public async Task<Resultado> EjecutarAsync(CambiarEstadoAbonadoSolicitud s, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(s.Estado)) return Resultado.Falla("El estado indicado no es valido.");

        var abonado = await repositorio.ObtenerConExpedienteAsync(s.AbonadoId, ct);
        if (abonado is null) return Resultado.Falla("El abonado no existe.");

        if (abonado.Estado == s.Estado) return Resultado.Ok();

        abonado.CambiarEstado(s.Estado);
        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}

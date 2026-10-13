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

        return Resultado<ReciboDto>.Ok(new ReciboDto(
            r.Id, r.Numero, r.Estado, periodo.Anio, periodo.Mes, r.FechaEmision, r.FechaVencimiento,
            org?.Nombre ?? "", org?.CedulaJuridica, org?.Telefono, org?.Correo,
            r.ServicioId, r.AbonadoCodigo, r.AbonadoNombre, r.PropiedadDireccion, r.CategoriaNombre,
            r.NumeroSerieMedidor, r.LecturaAnterior, r.LecturaActual, r.FechaLectura, r.Consumo,
            r.HuboCambioMedidor, r.LecturaRequiereRevision, r.MotivosRevision,
            r.Detalles.OrderBy(d => d.Orden)
                .Select(d => new LineaReciboDto(d.Tipo, d.Concepto, d.Cantidad, d.PrecioUnitario, d.Monto))
                .ToList(),
            r.SubtotalAgua, r.MontoHidrante, r.Total));
    }
}

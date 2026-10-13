using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Entidades;

namespace Asada.Aplicacion.Recibos;

internal static class MapeoRecibos
{
    /// <summary>Recibo emitido + su periodo + la ASADA emisora -> lo que se muestra o imprime.</summary>
    public static ReciboDto ADto(Recibo r, Periodo periodo, Organizacion? org) => new(
        r.Id, r.Numero, r.Estado, periodo.Anio, periodo.Mes, r.FechaEmision, r.FechaVencimiento,
        org?.Nombre ?? "", org?.CedulaJuridica, org?.Telefono, org?.Correo,
        r.ServicioId, r.AbonadoCodigo, r.AbonadoNombre, r.PropiedadDireccion, r.CategoriaNombre,
        r.NumeroSerieMedidor, r.LecturaAnterior, r.LecturaActual, r.FechaLectura, r.Consumo,
        r.HuboCambioMedidor, r.LecturaRequiereRevision, r.MotivosRevision,
        r.Detalles.OrderBy(d => d.Orden)
            .Select(d => new LineaReciboDto(d.Tipo, d.Concepto, d.Cantidad, d.PrecioUnitario, d.Monto))
            .ToList(),
        r.SubtotalAgua, r.MontoHidrante, r.Total);
}

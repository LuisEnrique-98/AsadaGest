using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;

namespace Asada.Aplicacion.Mediciones;

/// <summary>Una lectura vigente con sus datos de registro y si se puede corregir o anular (y por que no).</summary>
public class ObtenerLecturaCasoUso(IRepositorioMediciones repositorio)
{
    public async Task<Resultado<LecturaDetalleDto>> EjecutarAsync(int medicionId, CancellationToken ct = default)
    {
        var m = await repositorio.ObtenerMedicionAsync(medicionId, ct);
        if (m is null) return Resultado<LecturaDetalleDto>.Falla("La lectura no existe o fue anulada.");

        var servicio = await repositorio.ObtenerServicioAsync(m.ServicioId, ct);
        var periodo = await repositorio.ObtenerPeriodoPorIdAsync(m.PeriodoId, ct);
        var medidor = await repositorio.ObtenerMedidorAsync(m.MedidorId, ct);
        if (servicio?.Propiedad?.Abonado is null || periodo is null || medidor is null)
            return Resultado<LecturaDetalleDto>.Falla("La lectura no existe o fue anulada.");

        var recibo = await repositorio.ObtenerReciboEmitidoDeLecturaAsync(m.Id, ct);
        var razon = await ReglasModificacionLectura.RazonNoModificableAsync(repositorio, m, periodo, ct);
        var abonado = servicio.Propiedad.Abonado;

        return Resultado<LecturaDetalleDto>.Ok(new LecturaDetalleDto(
            m.Id, servicio.Id, abonado.Id, abonado.Codigo, abonado.Nombre, servicio.Propiedad.Direccion,
            periodo.Anio, periodo.Mes, periodo.Estado, medidor.NumeroSerie,
            m.FechaLectura, m.Lectura, m.Consumo, m.RequiereRevision, m.MotivosRevision, m.Observaciones,
            m.FechaCreacion, m.RegistradaPorNombre, m.FechaUltimaCorreccion, m.CorregidaPorNombre, m.MotivoCorreccion,
            recibo?.Id, recibo?.Numero, razon));
    }
}

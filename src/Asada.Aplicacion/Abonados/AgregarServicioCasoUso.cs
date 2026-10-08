using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;

namespace Asada.Aplicacion.Abonados;

public class AgregarServicioCasoUso(IRepositorioAbonados repositorio)
{
    public async Task<Resultado<int>> EjecutarAsync(AgregarServicioSolicitud s, CancellationToken ct = default)
    {
        var errores = new List<string>();

        if (s.FechaInicio == default) errores.Add("La fecha de inicio es obligatoria.");
        Validacion.Largo(errores, s.Observaciones, 400, "Las observaciones");
        if (s.MensualidadesPendientes < 0) errores.Add("Las mensualidades pendientes no pueden ser negativas.");
        if (s.MontoPendiente < 0) errores.Add("El monto pendiente no puede ser negativo.");
        if (!s.EsMoroso && (s.MensualidadesPendientes > 0 || s.MontoPendiente > 0))
            errores.Add("Si hay mensualidades o monto pendiente, el servicio debe marcarse como moroso.");
        if (errores.Count > 0) return Resultado<int>.Falla(errores);

        var abonado = await repositorio.ObtenerConExpedienteAsync(s.AbonadoId, ct);
        if (abonado is null) return Resultado<int>.Falla("El abonado no existe.");

        var propiedad = abonado.Propiedades.FirstOrDefault(p => p.Id == s.PropiedadId);
        if (propiedad is null) return Resultado<int>.Falla("La propiedad no existe o no pertenece a este abonado.");

        var servicio = propiedad.AgregarServicio(
            s.FechaInicio, Validacion.Limpiar(s.Observaciones),
            s.EsMoroso, s.MensualidadesPendientes, s.MontoPendiente);

        await repositorio.GuardarCambiosAsync(ct);
        return Resultado<int>.Ok(servicio.Id);
    }
}

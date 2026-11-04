using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Anula una lectura registrada por error (otro servicio, otro periodo, lectura inventada...).
/// Solo el Administrador, con motivo. La lectura no se borra: queda anulada, deja de contar para
/// el consumo y la facturacion, y el servicio queda sin lectura en ese periodo, listo para
/// registrarla de nuevo. Ver ReglasModificacionLectura para cuando se permite.
/// </summary>
public class AnularLecturaCasoUso(IRepositorioMediciones repositorio, IProveedorUsuario usuario)
{
    public async Task<Resultado> EjecutarAsync(AnularLecturaSolicitud s, CancellationToken ct = default)
    {
        if (ReglasMotivo.Validar(s.Motivo) is string errorMotivo) return Resultado.Falla(errorMotivo);

        var medicion = await repositorio.ObtenerMedicionAsync(s.MedicionId, ct);
        if (medicion is null) return Resultado.Falla("La lectura no existe o ya fue anulada.");

        var periodo = await repositorio.ObtenerPeriodoPorIdAsync(medicion.PeriodoId, ct);
        if (periodo is null) return Resultado.Falla("La lectura no existe o ya fue anulada.");

        if (await ReglasModificacionLectura.RazonNoModificableAsync(repositorio, medicion, periodo, ct) is string razon)
            return Resultado.Falla(razon);

        medicion.Anular(s.Motivo, usuario.UsuarioId, usuario.Nombre, DateTime.UtcNow);
        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}

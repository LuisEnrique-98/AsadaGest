using Asada.Aplicacion.Common;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Recibos;

/// <summary>
/// Anula un recibo emitido (solo el Administrador, con motivo; decision del 13 de octubre de 2026).
/// El recibo no se borra: conserva su numero, se imprime marcado ANULADO y queda en la auditoria.
/// El servicio vuelve a estar listo para facturar en ese periodo; si la lectura estaba mal, se
/// corrige primero y despues se genera el recibo nuevo (con otro numero). No se anulan recibos
/// de un periodo cerrado.
/// </summary>
public class AnularReciboCasoUso(IRepositorioRecibos repositorio, IProveedorUsuario usuario)
{
    public async Task<Resultado> EjecutarAsync(AnularReciboSolicitud s, CancellationToken ct = default)
    {
        if (ReglasMotivo.Validar(s.Motivo) is string errorMotivo) return Resultado.Falla(errorMotivo);

        if (await repositorio.ObtenerReciboParaModificarAsync(s.ReciboId, ct) is not var (recibo, periodo))
            return Resultado.Falla("El recibo no existe.");

        if (recibo.Estado != EstadoRecibo.Emitido)
            return Resultado.Falla($"El recibo {recibo.Numero} ya esta anulado.");

        if (periodo.EstaCerrado)
            return Resultado.Falla(
                $"El periodo {periodo.Mes:00}/{periodo.Anio} esta cerrado; no se pueden anular sus recibos. Un Administrador debe reabrirlo primero.");

        recibo.Anular(s.Motivo, usuario.UsuarioId, usuario.Nombre, DateTime.UtcNow);
        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}

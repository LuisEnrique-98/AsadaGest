using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Cuando se puede corregir o anular una lectura (decisiones del 13 de octubre de 2026):
///   - el periodo debe estar abierto;
///   - no debe tener un recibo emitido: primero se anula el recibo, luego se corrige la lectura y
///     se genera un recibo nuevo;
///   - debe ser la ultima lectura del servicio: el consumo de la lectura siguiente se calculo a
///     partir de esta, asi que cambiarla dejaria mal ese consumo (y quiza su recibo).
/// </summary>
internal static class ReglasModificacionLectura
{
    public static async Task<string?> RazonNoModificableAsync(
        IRepositorioMediciones repositorio, Medicion medicion, Periodo periodo, CancellationToken ct)
    {
        if (periodo.EstaCerrado)
            return $"El periodo {periodo.Mes:00}/{periodo.Anio} esta cerrado. Un Administrador debe reabrirlo primero.";

        if (await repositorio.ObtenerReciboEmitidoDeLecturaAsync(medicion.Id, ct) is ReciboDeLectura recibo)
            return $"La lectura ya se facturo en el recibo {recibo.Numero}. Anule primero ese recibo.";

        var ultima = await repositorio.ObtenerUltimaLecturaAsync(medicion.ServicioId, ct);
        if (ultima is not null && ReglasPeriodo.Comparar(ultima.Anio, ultima.Mes, periodo.Anio, periodo.Mes) > 0)
            return $"El servicio ya tiene una lectura posterior ({ultima.Mes:00}/{ultima.Anio}), calculada a partir de esta. Solo se puede corregir o anular la ultima lectura del servicio.";

        return null;
    }
}

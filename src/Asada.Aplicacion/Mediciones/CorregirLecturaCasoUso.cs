using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Corrige la fecha, el valor o las observaciones de una lectura (solo el Administrador, con
/// motivo). El consumo y los motivos de revision se recalculan con las mismas reglas que al
/// registrarla. El periodo no cambia: si la lectura se registro en el periodo equivocado, se
/// anula y se registra de nuevo. Ver ReglasModificacionLectura para cuando se permite.
/// </summary>
public class CorregirLecturaCasoUso(
    IRepositorioMediciones repositorio, IProveedorFecha fecha, IProveedorUsuario usuario)
{
    public async Task<Resultado<LecturaRegistradaDto>> EjecutarAsync(CorregirLecturaSolicitud s, CancellationToken ct = default)
    {
        var errores = new List<string>();
        if (s.Lectura < 0) errores.Add("La lectura no puede ser negativa.");
        if (s.FechaLectura == default) errores.Add("La fecha de lectura es obligatoria.");
        else if (s.FechaLectura > fecha.Hoy) errores.Add("La fecha de lectura no puede ser futura.");
        Validacion.Largo(errores, s.Observaciones, 400, "Las observaciones");
        if (ReglasMotivo.Validar(s.Motivo) is string errorMotivo) errores.Add(errorMotivo);
        if (errores.Count > 0) return Resultado<LecturaRegistradaDto>.Falla(errores);

        var medicion = await repositorio.ObtenerMedicionAsync(s.MedicionId, ct);
        if (medicion is null) return Resultado<LecturaRegistradaDto>.Falla("La lectura no existe o fue anulada.");

        var periodo = await repositorio.ObtenerPeriodoPorIdAsync(medicion.PeriodoId, ct);
        if (periodo is null) return Resultado<LecturaRegistradaDto>.Falla("La lectura no existe o fue anulada.");

        if (await ReglasModificacionLectura.RazonNoModificableAsync(repositorio, medicion, periodo, ct) is string razon)
            return Resultado<LecturaRegistradaDto>.Falla(razon);

        var medidores = await repositorio.ListarMedidoresDeServicioAsync(medicion.ServicioId, ct);
        var medidor = medidores.FirstOrDefault(m => m.Id == medicion.MedidorId);
        if (medidor is null) return Resultado<LecturaRegistradaDto>.Falla("El historial de medidores del servicio es inconsistente.");

        // Si el medidor se retiro despues de esta lectura, la lectura corregida no puede pasar de su
        // lectura final ni de su fecha de retiro (el tramo siguiente quedaria negativo).
        if (medidor.Estado == EstadoMedidor.Retirado)
        {
            if (s.Lectura > medidor.LecturaFinal)
                errores.Add($"La lectura no puede ser mayor que la lectura final del medidor {medidor.NumeroSerie} ({medidor.LecturaFinal:0.###}), que ya fue retirado.");
            if (s.FechaLectura > medidor.FechaRetiro)
                errores.Add($"La fecha de lectura no puede ser posterior al retiro del medidor {medidor.NumeroSerie} ({medidor.FechaRetiro:dd/MM/yyyy}).");
            if (errores.Count > 0) return Resultado<LecturaRegistradaDto>.Falla(errores);
        }

        var anterior = await repositorio.ObtenerLecturaAnteriorAAsync(medicion.ServicioId, periodo.Anio, periodo.Mes, ct);
        var previos = await repositorio.ObtenerConsumosAnterioresAAsync(
            medicion.ServicioId, periodo.Anio, periodo.Mes, ReglasConsumoAnormal.PeriodosReferencia, ct);
        var umbral = await repositorio.ObtenerPorcentajeConsumoAnormalAsync(ct);

        var evaluacion = EvaluadorLectura.Evaluar(
            periodo.Anio, periodo.Mes, s.FechaLectura, s.Lectura, medidor, medidores, anterior, previos, umbral);
        if (!evaluacion.EsExitoso) return Resultado<LecturaRegistradaDto>.Falla(evaluacion.Errores);
        var (consumo, motivos) = evaluacion.Valor!;

        medicion.Corregir(s.FechaLectura, s.Lectura, consumo, motivos, Validacion.Limpiar(s.Observaciones),
            s.Motivo, usuario.UsuarioId, usuario.Nombre, DateTime.UtcNow);
        await repositorio.GuardarCambiosAsync(ct);

        return Resultado<LecturaRegistradaDto>.Ok(new LecturaRegistradaDto(
            medicion.Id, medicion.Lectura, medicion.Consumo, medicion.RequiereRevision, motivos));
    }
}

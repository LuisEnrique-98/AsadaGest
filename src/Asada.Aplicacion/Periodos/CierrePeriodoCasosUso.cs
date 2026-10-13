using Asada.Aplicacion.Common;
using Asada.Aplicacion.Periodos.Dtos;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Periodos;

/// <summary>
/// Reglas del cierre de un periodo (decision del 13 de octubre de 2026): lo cierra el
/// Administrador cuando termino de facturarlo; cerrado, no se registran ni corrigen lecturas ni
/// se generan o anulan recibos; solo el Administrador lo reabre, con motivo.
///
/// Para cerrar se exige:
///   - que ya haya pasado la ventana de lectura (ultima semana del mes);
///   - que ningun servicio tenga lectura del periodo sin recibo emitido. Si se cerrara asi, ese
///     consumo no se cobraria nunca: la lectura siguiente se calcula a partir de esta. Los
///     servicios SIN lectura no impiden el cierre: su consumo se cobra completo en la proxima
///     lectura (decision del 9 de octubre).
/// </summary>
internal static class ReglasCierrePeriodo
{
    public static async Task<(CierrePeriodoDto Dto, Dominio.Entidades.Periodo? Periodo)> EvaluarAsync(
        IRepositorioPeriodos periodos, IRepositorioRecibos recibos, IProveedorFecha fecha,
        int anio, int mes, CancellationToken ct)
    {
        var periodo = await periodos.ObtenerPeriodoAsync(anio, mes, ct);
        var servicios = await recibos.ListarServiciosDelPeriodoAsync(anio, mes, ct);
        var tarifas = await recibos.ListarTarifasAsync(ct);
        var anulados = await recibos.ListarAnuladosPeriodoAsync(anio, mes, ct);

        var estados = servicios
            .Select(s => (Servicio: s, Estado: ObtenerFacturacionPeriodoCasoUso.Clasificar(
                s, ObtenerFacturacionPeriodoCasoUso.TarifaDelPeriodo(tarifas, s.CategoriaId, anio, mes))))
            .ToList();

        var conRecibo = estados.Count(e => e.Estado == EstadoFacturacion.ConRecibo);
        var sinLectura = estados.Count(e => e.Estado == EstadoFacturacion.SinLectura);
        var conLecturaSinRecibo = estados.Count(e => e.Estado is EstadoFacturacion.ListoParaFacturar
            or EstadoFacturacion.SinCategoria or EstadoFacturacion.SinTarifa);
        var monto = estados.Where(e => e.Estado == EstadoFacturacion.ConRecibo).Sum(e => e.Servicio.ReciboTotal ?? 0m);

        var ventana = ReglasPeriodo.EstadoVentana(anio, mes, fecha.Hoy);
        var impedimentos = new List<string>();
        if (ventana != EstadoVentanaLectura.Cerrada)
            impedimentos.Add(
                $"La ventana de lectura termina el {ReglasPeriodo.FinVentana(anio, mes):dd/MM/yyyy}; el periodo se puede cerrar a partir del dia siguiente.");
        if (conLecturaSinRecibo > 0)
            impedimentos.Add(
                $"{conLecturaSinRecibo} servicio(s) tienen lectura del periodo pero no recibo. Genere sus recibos (o anule la lectura) antes de cerrar: si no, ese consumo no se cobraria.");

        var dto = new CierrePeriodoDto(
            anio, mes, periodo?.Estado ?? EstadoPeriodo.Abierto, ventana, ReglasPeriodo.FinVentana(anio, mes),
            periodo?.FechaCierre, periodo?.CerradoPorNombre,
            periodo?.FechaReapertura, periodo?.ReabiertoPorNombre, periodo?.MotivoReapertura,
            servicios.Count, conRecibo, monto, sinLectura, conLecturaSinRecibo, anulados.Count, impedimentos);
        return (dto, periodo);
    }
}

public class ObtenerCierrePeriodoCasoUso(IRepositorioPeriodos periodos, IRepositorioRecibos recibos, IProveedorFecha fecha)
{
    public async Task<Resultado<CierrePeriodoDto>> EjecutarAsync(int anio, int mes, CancellationToken ct = default)
    {
        if (!ReglasPeriodo.EsMesValido(anio, mes))
            return Resultado<CierrePeriodoDto>.Falla("El periodo indicado no es valido.");

        var (dto, _) = await ReglasCierrePeriodo.EvaluarAsync(periodos, recibos, fecha, anio, mes, ct);
        return Resultado<CierrePeriodoDto>.Ok(dto);
    }
}

public class CerrarPeriodoCasoUso(
    IRepositorioPeriodos periodos, IRepositorioRecibos recibos, IProveedorFecha fecha,
    IProveedorOrganizacion proveedorOrganizacion, IProveedorUsuario usuario)
{
    public async Task<Resultado> EjecutarAsync(int anio, int mes, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is null)
            return Resultado.Falla("Su usuario no pertenece a ninguna ASADA; no puede cerrar periodos.");
        if (!ReglasPeriodo.EsMesValido(anio, mes))
            return Resultado.Falla("El periodo indicado no es valido.");

        var (dto, periodo) = await ReglasCierrePeriodo.EvaluarAsync(periodos, recibos, fecha, anio, mes, ct);
        if (dto.EstaCerrado) return Resultado.Falla($"El periodo {mes:00}/{anio} ya esta cerrado.");
        if (dto.Impedimentos.Count > 0) return Resultado.Falla(dto.Impedimentos);

        // Un mes sin ninguna lectura todavia no tiene fila de periodo: se crea para poder cerrarlo.
        periodo ??= await periodos.ObtenerOCrearPeriodoAsync(anio, mes, ct);
        periodo.Cerrar(usuario.UsuarioId, usuario.Nombre, DateTime.UtcNow);
        await periodos.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}

public class ReabrirPeriodoCasoUso(IRepositorioPeriodos periodos, IProveedorUsuario usuario)
{
    public async Task<Resultado> EjecutarAsync(ReabrirPeriodoSolicitud s, CancellationToken ct = default)
    {
        if (!ReglasPeriodo.EsMesValido(s.Anio, s.Mes)) return Resultado.Falla("El periodo indicado no es valido.");
        if (ReglasMotivo.Validar(s.Motivo) is string errorMotivo) return Resultado.Falla(errorMotivo);

        var periodo = await periodos.ObtenerPeriodoAsync(s.Anio, s.Mes, ct);
        if (periodo is null || !periodo.EstaCerrado)
            return Resultado.Falla($"El periodo {s.Mes:00}/{s.Anio} no esta cerrado.");

        periodo.Reabrir(s.Motivo, usuario.UsuarioId, usuario.Nombre, DateTime.UtcNow);
        await periodos.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}

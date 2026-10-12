using Asada.Aplicacion.Common;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Recibos;

/// <summary>
/// Estado de la facturacion de un periodo: que servicios ya tienen recibo, cuales estan listos
/// para facturar y por que los demas todavia no se pueden facturar.
///
/// Decisiones del 12 de octubre de 2026:
///   - Servicio sin lectura del periodo: NO se le genera recibo; queda pendiente hasta que se
///     registre la lectura.
///   - Lectura marcada para revision: SI se factura (el consumo se cobra completo, decision del
///     9 de octubre), pero el recibo queda senalado.
/// </summary>
public class ObtenerFacturacionPeriodoCasoUso(IRepositorioRecibos repositorio)
{
    public async Task<Resultado<FacturacionPeriodoDto>> EjecutarAsync(int anio, int mes, CancellationToken ct = default)
    {
        if (!ReglasPeriodo.EsMesValido(anio, mes))
            return Resultado<FacturacionPeriodoDto>.Falla("El periodo indicado no es valido.");

        var servicios = await repositorio.ListarServiciosDelPeriodoAsync(anio, mes, ct);
        var tarifas = await repositorio.ListarTarifasAsync(ct);

        var filas = servicios.Select(s =>
        {
            var estado = Clasificar(s, TarifaDelPeriodo(tarifas, s.CategoriaId, anio, mes));
            return new FilaFacturacionDto(
                s.ServicioId, s.AbonadoId, s.AbonadoCodigo, s.AbonadoNombre, s.PropiedadDireccion,
                s.CategoriaNombre, s.Consumo, s.LecturaRequiereRevision, estado,
                s.ReciboId, s.ReciboNumero, s.ReciboTotal);
        }).ToList();

        return Resultado<FacturacionPeriodoDto>.Ok(new FacturacionPeriodoDto(anio, mes, filas));
    }

    internal static Tarifa? TarifaDelPeriodo(IReadOnlyList<Tarifa> tarifas, int? categoriaId, int anio, int mes)
        => categoriaId is int c
            ? ReglasTarifa.SeleccionarVigente(tarifas.Where(t => t.CategoriaId == c), ReglasTarifa.FechaReferencia(anio, mes))
            : null;

    internal static EstadoFacturacion Clasificar(ServicioAFacturar s, Tarifa? tarifa)
    {
        if (s.ReciboId is not null) return EstadoFacturacion.ConRecibo;
        if (s.MedicionId is null) return EstadoFacturacion.SinLectura;
        if (s.CategoriaId is null) return EstadoFacturacion.SinCategoria;
        if (tarifa is null) return EstadoFacturacion.SinTarifa;
        return EstadoFacturacion.ListoParaFacturar;
    }
}

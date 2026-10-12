using Asada.Aplicacion.Common;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Recibos;

/// <summary>
/// Genera los recibos de un periodo para todos los servicios que esten listos para facturar.
///
/// Se puede ejecutar varias veces sobre el mismo periodo: los servicios que ya tienen recibo se
/// saltan, asi que una segunda corrida solo factura los que se pusieron al dia despues (lectura
/// registrada tarde, categoria asignada, tarifa cargada). Todos los recibos de una corrida se
/// guardan juntos o ninguno.
/// </summary>
public class GenerarRecibosPeriodoCasoUso(
    IRepositorioRecibos repositorio, IProveedorOrganizacion proveedorOrganizacion, IProveedorFecha fecha)
{
    public async Task<Resultado<GeneracionRecibosDto>> EjecutarAsync(GenerarRecibosSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<GeneracionRecibosDto>.Falla("Su usuario no pertenece a ninguna ASADA; no puede generar recibos.");

        var errores = new List<string>();
        if (!ReglasPeriodo.EsMesValido(s.Anio, s.Mes)) errores.Add("El periodo indicado no es valido.");
        if (s.FechaVencimiento == default) errores.Add("Indique la fecha de vencimiento de los recibos.");
        else if (s.FechaVencimiento < fecha.Hoy) errores.Add("La fecha de vencimiento no puede ser anterior a hoy.");
        if (errores.Count > 0) return Resultado<GeneracionRecibosDto>.Falla(errores);

        var periodo = await repositorio.ObtenerPeriodoAsync(s.Anio, s.Mes, ct);
        if (periodo is null)
            return Resultado<GeneracionRecibosDto>.Falla("El periodo no tiene lecturas registradas; no hay nada que facturar.");

        var servicios = await repositorio.ListarServiciosDelPeriodoAsync(s.Anio, s.Mes, ct);
        var tarifas = await repositorio.ListarTarifasAsync(ct);

        var recibos = new List<Recibo>();
        var pendientes = 0;

        foreach (var servicio in servicios)
        {
            var tarifa = ObtenerFacturacionPeriodoCasoUso.TarifaDelPeriodo(tarifas, servicio.CategoriaId, s.Anio, s.Mes);
            var estado = ObtenerFacturacionPeriodoCasoUso.Clasificar(servicio, tarifa);
            if (estado == EstadoFacturacion.ConRecibo) continue;
            if (estado != EstadoFacturacion.ListoParaFacturar) { pendientes++; continue; }

            var calculo = CalculadoraRecibo.Calcular(tarifa!, servicio.Consumo!.Value);
            if (!calculo.EsValido)
                return Resultado<GeneracionRecibosDto>.Falla(
                    $"No se pudo calcular el recibo de {servicio.AbonadoCodigo}: {calculo.Error}");

            recibos.Add(CrearRecibo(organizacionId, periodo.Id, servicio, tarifa!, calculo.Desglose!, s.FechaVencimiento));
        }

        if (recibos.Count == 0)
            return Resultado<GeneracionRecibosDto>.Ok(new GeneracionRecibosDto(0, 0, 0m, pendientes));

        if (!await repositorio.AgregarRecibosAsync(s.Anio, recibos, ct))
            return Resultado<GeneracionRecibosDto>.Falla(
                "Otro usuario genero recibos de este periodo al mismo tiempo. No se guardo nada; vuelva a intentarlo.");

        return Resultado<GeneracionRecibosDto>.Ok(new GeneracionRecibosDto(
            recibos.Count, recibos.Count(r => r.LecturaRequiereRevision), recibos.Sum(r => r.Total), pendientes));
    }

    private Recibo CrearRecibo(int organizacionId, int periodoId, ServicioAFacturar s, Tarifa tarifa,
        DesgloseRecibo desglose, DateOnly vencimiento) => new()
    {
        OrganizacionId = organizacionId,
        ServicioId = s.ServicioId,
        PeriodoId = periodoId,
        MedicionId = s.MedicionId!.Value,
        TarifaId = tarifa.Id,
        FechaEmision = fecha.Hoy,
        FechaVencimiento = vencimiento,
        AbonadoCodigo = s.AbonadoCodigo,
        AbonadoNombre = s.AbonadoNombre,
        PropiedadDireccion = s.PropiedadDireccion,
        CategoriaNombre = s.CategoriaNombre ?? "",
        NumeroSerieMedidor = s.NumeroSerieMedidor ?? "",
        LecturaAnterior = s.LecturaAnterior,
        LecturaActual = s.Lectura!.Value,
        FechaLectura = s.FechaLectura!.Value,
        Consumo = s.Consumo!.Value,
        HuboCambioMedidor = s.MedidorAnteriorId is int anterior && anterior != s.MedidorId,
        LecturaRequiereRevision = s.LecturaRequiereRevision,
        MotivosRevision = s.MotivosRevision,
        SubtotalAgua = desglose.SubtotalAgua,
        MontoHidrante = desglose.MontoHidrante,
        Total = desglose.Total,
        Detalles = desglose.Lineas.Select((l, i) => new ReciboDetalle
        {
            Orden = i + 1,
            Tipo = l.Tipo,
            Concepto = l.Concepto,
            Cantidad = l.Cantidad,
            PrecioUnitario = l.PrecioUnitario,
            Monto = l.Monto,
        }).ToList(),
    };
}

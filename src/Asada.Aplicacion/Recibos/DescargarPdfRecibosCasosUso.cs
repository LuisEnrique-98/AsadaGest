using Asada.Aplicacion.Common;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Recibos;

/// <summary>PDF de un recibo, para reimprimirlo o enviarlo. Sirve tambien para recibos anulados
/// (el PDF los marca como tales), porque son parte del historial.</summary>
public class DescargarPdfReciboCasoUso(IRepositorioRecibos repositorio, IGeneradorPdfRecibos generador)
{
    public async Task<Resultado<ArchivoDescarga>> EjecutarAsync(int reciboId, CancellationToken ct = default)
    {
        if (await repositorio.ObtenerReciboAsync(reciboId, ct) is not var (r, periodo))
            return Resultado<ArchivoDescarga>.Falla("El recibo no existe o no pertenece a su ASADA.");

        var org = await repositorio.ObtenerOrganizacionAsync(r.OrganizacionId, ct);
        var pdf = generador.Generar([MapeoRecibos.ADto(r, periodo, org)]);
        return Resultado<ArchivoDescarga>.Ok(ArchivoDescarga.Pdf($"{r.Numero}.pdf", pdf));
    }
}

/// <summary>
/// Un solo PDF con todos los recibos emitidos de un periodo, dos por hoja carta, para imprimirlos
/// de una vez y repartirlos. Van en el mismo orden que el listado de la pantalla Recibos (por
/// nombre del abonado). Los recibos anulados no se incluyen.
/// </summary>
public class DescargarPdfRecibosPeriodoCasoUso(
    IRepositorioRecibos repositorio, IProveedorOrganizacion proveedorOrganizacion, IGeneradorPdfRecibos generador)
{
    public async Task<Resultado<ArchivoDescarga>> EjecutarAsync(int anio, int mes, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<ArchivoDescarga>.Falla("Su usuario no pertenece a ninguna ASADA; no puede imprimir recibos.");
        if (!ReglasPeriodo.EsMesValido(anio, mes))
            return Resultado<ArchivoDescarga>.Falla("El periodo indicado no es valido.");

        var periodo = await repositorio.ObtenerPeriodoAsync(anio, mes, ct);
        var recibos = periodo is null
            ? []
            : (await repositorio.ListarRecibosPeriodoAsync(anio, mes, ct))
                .Where(r => r.Estado == EstadoRecibo.Emitido)
                .ToList();
        if (recibos.Count == 0)
            return Resultado<ArchivoDescarga>.Falla("No hay recibos emitidos en este periodo; primero genere los recibos.");

        var org = await repositorio.ObtenerOrganizacionAsync(organizacionId, ct);
        var dtos = recibos
            .OrderBy(r => r.AbonadoNombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Numero, StringComparer.Ordinal)
            .Select(r => MapeoRecibos.ADto(r, periodo!, org))
            .ToList();

        var pdf = generador.Generar(dtos);
        var codigo = string.IsNullOrWhiteSpace(org?.Codigo) ? "ASADA" : org!.Codigo;
        return Resultado<ArchivoDescarga>.Ok(ArchivoDescarga.Pdf($"Recibos-{codigo}-{anio}-{mes:00}.pdf", pdf));
    }
}

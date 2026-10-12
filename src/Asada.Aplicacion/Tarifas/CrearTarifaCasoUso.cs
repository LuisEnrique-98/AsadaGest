using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Tarifas.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Tarifas;

/// <summary>
/// Registra una version nueva de la tarifa de una categoria. Las versiones no se editan: un cambio
/// de precios es siempre una version nueva (SRS: tarifas versionadas para reconstruir recibos).
///
/// Reglas de la fecha "vigente desde":
///   - Debe ser posterior a la de la ultima version de la categoria (las versiones van en orden).
///   - Debe ser posterior al ultimo mes ya facturado con esa categoria: si no, recibos del mismo
///     periodo quedarian calculados con dos tarifas distintas segun cuando se generaron.
/// </summary>
public class CrearTarifaCasoUso(IRepositorioTarifas repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<int>> EjecutarAsync(CrearTarifaSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<int>.Falla("Su usuario no pertenece a ninguna ASADA; no puede registrar tarifas.");

        var errores = new List<string>();
        if (s.VigenteDesde == default) errores.Add("Indique desde que fecha rige la tarifa.");
        errores.AddRange(ReglasTarifa.Validar(s.CargoFijo, s.ModoHidrante, s.ValorHidrante, s.Tramos));
        Validacion.Largo(errores, Validacion.Limpiar(s.Observaciones), 400, "Las observaciones");
        if (errores.Count > 0) return Resultado<int>.Falla(errores);

        var categoria = await repositorio.ObtenerCategoriaAsync(s.CategoriaId, ct);
        if (categoria is null) return Resultado<int>.Falla("La categoria no existe.");
        if (categoria.Estado != EstadoRegistro.Activo)
            return Resultado<int>.Falla("La categoria esta inactiva; no se le pueden registrar tarifas.");

        var versiones = (await repositorio.ListarTarifasAsync(ct)).Where(t => t.CategoriaId == s.CategoriaId).ToList();
        if (versiones.Count > 0)
        {
            var ultima = versiones.Max(t => t.VigenteDesde);
            if (s.VigenteDesde <= ultima)
                return Resultado<int>.Falla(
                    $"La categoria ya tiene una tarifa vigente desde el {ultima:dd/MM/yyyy}. La version nueva debe regir desde una fecha posterior.");
        }

        if (await repositorio.ObtenerUltimoPeriodoFacturadoAsync(s.CategoriaId, ct) is (int anio, int mes))
        {
            var finFacturado = ReglasTarifa.FechaReferencia(anio, mes);
            if (s.VigenteDesde <= finFacturado)
                return Resultado<int>.Falla(
                    $"Ya se emitieron recibos de {mes:00}/{anio} con esta categoria. La version nueva debe regir a partir del {finFacturado.AddDays(1):dd/MM/yyyy}.");
        }

        var tarifa = new Tarifa
        {
            OrganizacionId = organizacionId,
            CategoriaId = s.CategoriaId,
            VigenteDesde = s.VigenteDesde,
            CargoFijo = s.CargoFijo,
            ModoHidrante = s.ModoHidrante,
            ValorHidrante = s.ModoHidrante == ModoHidrante.NoAplica ? 0m : s.ValorHidrante,
            Observaciones = Validacion.Limpiar(s.Observaciones),
            Bloques = s.Tramos.Select((t, i) => new TarifaBloque
            {
                Orden = i + 1,
                LimiteSuperior = t.LimiteSuperior,
                PrecioPorMetroCubico = t.PrecioPorMetroCubico,
            }).ToList(),
        };

        await repositorio.AgregarTarifaAsync(tarifa, ct);
        return Resultado<int>.Ok(tarifa.Id);
    }
}

using Asada.Aplicacion.Common;
using Asada.Aplicacion.Tarifas.Dtos;

namespace Asada.Aplicacion.Tarifas;

/// <summary>Categorias de la ASADA con todas sus versiones de tarifa, marcando la que rige hoy.</summary>
public class ObtenerPanelTarifasCasoUso(IRepositorioTarifas repositorio, IProveedorFecha fecha)
{
    public async Task<PanelTarifasDto> EjecutarAsync(CancellationToken ct = default)
    {
        var categorias = await repositorio.ListarCategoriasAsync(ct);
        var conteos = await repositorio.ContarServiciosPorCategoriaAsync(ct);
        var sinCategoria = await repositorio.ContarServiciosActivosSinCategoriaAsync(ct);
        var tarifas = await repositorio.ListarTarifasAsync(ct);
        var vigentes = MapeoTarifas.IdsVigentes(tarifas, fecha.Hoy);

        var filas = categorias.Select(c => new CategoriaConTarifasDto(
            new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.Estado, conteos.GetValueOrDefault(c.Id)),
            tarifas.Where(t => t.CategoriaId == c.Id)
                .OrderByDescending(t => t.VigenteDesde)
                .Select(t => new TarifaResumenDto(
                    t.Id, t.VigenteDesde, t.CargoFijo, t.ModoHidrante, t.ValorHidrante, t.Bloques.Count,
                    vigentes.Contains(t.Id), t.VigenteDesde > fecha.Hoy))
                .ToList()))
            .ToList();

        return new PanelTarifasDto(filas, sinCategoria);
    }
}

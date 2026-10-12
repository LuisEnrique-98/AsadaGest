using Asada.Aplicacion.Tarifas.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Tarifas;

internal static class MapeoTarifas
{
    public static IReadOnlyList<TramoDto> Tramos(Tarifa t)
    {
        var tramos = new List<TramoDto>();
        decimal desde = 0m;
        foreach (var b in t.Bloques.OrderBy(b => b.Orden))
        {
            tramos.Add(new TramoDto(b.Orden, desde, b.LimiteSuperior, b.PrecioPorMetroCubico,
                ReglasTarifa.DescribirTramo(desde, b.LimiteSuperior)));
            desde = b.LimiteSuperior ?? desde;
        }
        return tramos;
    }

    /// <summary>Id de la version que rige hoy en cada categoria.</summary>
    public static HashSet<int> IdsVigentes(IEnumerable<Tarifa> tarifas, DateOnly hoy)
        => tarifas.GroupBy(t => t.CategoriaId)
            .Select(g => ReglasTarifa.SeleccionarVigente(g, hoy)?.Id)
            .OfType<int>()
            .ToHashSet();
}

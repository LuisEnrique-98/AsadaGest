using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Tarifas.Dtos;

public sealed record CategoriaDto(int Id, string Nombre, string? Descripcion, EstadoRegistro Estado, int CantidadServicios);

public sealed record TramoDto(int Orden, decimal Desde, decimal? Hasta, decimal PrecioPorMetroCubico, string Descripcion);

public sealed record TarifaResumenDto(
    int Id,
    DateOnly VigenteDesde,
    decimal CargoFijo,
    ModoHidrante ModoHidrante,
    decimal ValorHidrante,
    int CantidadTramos,
    /// <summary>True si es la version que rige hoy para su categoria.</summary>
    bool RigeHoy,
    /// <summary>True si todavia no entra a regir.</summary>
    bool EsFutura);

public sealed record CategoriaConTarifasDto(CategoriaDto Categoria, IReadOnlyList<TarifaResumenDto> Versiones)
{
    public TarifaResumenDto? VersionVigente => Versiones.FirstOrDefault(v => v.RigeHoy);
}

public sealed record PanelTarifasDto(IReadOnlyList<CategoriaConTarifasDto> Categorias, int ServiciosSinCategoria);

public sealed record TarifaDetalleDto(
    int Id,
    int CategoriaId,
    string CategoriaNombre,
    DateOnly VigenteDesde,
    decimal CargoFijo,
    ModoHidrante ModoHidrante,
    decimal ValorHidrante,
    string? Observaciones,
    IReadOnlyList<TramoDto> Tramos,
    bool RigeHoy,
    bool EsFutura);

public sealed record CategoriaServicioDto(
    int ServicioId,
    int AbonadoId,
    string AbonadoCodigo,
    string AbonadoNombre,
    string PropiedadDireccion,
    int? CategoriaId,
    IReadOnlyList<CategoriaDto> CategoriasDisponibles);

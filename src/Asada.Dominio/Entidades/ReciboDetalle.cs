using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>Linea del desglose de un recibo, congelada al momento de la emision.</summary>
public class ReciboDetalle
{
    public int Id { get; set; }
    public int ReciboId { get; set; }
    public int Orden { get; set; }
    public TipoLineaRecibo Tipo { get; set; }
    public required string Concepto { get; set; }

    /// <summary>m³ cobrados en la linea (null en cargos que no dependen del consumo).</summary>
    public decimal? Cantidad { get; set; }

    /// <summary>Precio por m³ (null en cargos que no dependen del consumo).</summary>
    public decimal? PrecioUnitario { get; set; }

    public decimal Monto { get; set; }
}

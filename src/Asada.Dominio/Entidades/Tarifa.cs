using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Una version de la tarifa de una categoria. Las tarifas son versionadas e inmutables: un cambio
/// de precios se registra como una version nueva con su propia fecha de vigencia, nunca editando
/// la anterior. Asi un recibo historico siempre se puede reconstruir con la tarifa que se uso
/// (el recibo guarda TarifaId y, ademas, su propio desglose congelado).
/// </summary>
public class Tarifa
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int CategoriaId { get; set; }
    public CategoriaServicio? Categoria { get; set; }

    /// <summary>Primer dia en que rige esta version. Rige hasta el dia anterior a la siguiente version.</summary>
    public DateOnly VigenteDesde { get; set; }

    /// <summary>Cargo fijo mensual, se cobra aunque el consumo sea 0.</summary>
    public decimal CargoFijo { get; set; }

    public ModoHidrante ModoHidrante { get; set; } = ModoHidrante.NoAplica;

    /// <summary>Monto por m³ o monto fijo, segun ModoHidrante. Es 0 cuando no aplica.</summary>
    public decimal ValorHidrante { get; set; }

    public string? Observaciones { get; set; }

    public List<TarifaBloque> Bloques { get; set; } = [];

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

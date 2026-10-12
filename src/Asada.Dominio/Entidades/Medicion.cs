namespace Asada.Dominio.Entidades;

/// <summary>
/// Lectura de un servicio en un periodo, junto con el consumo ya calculado.
/// El consumo se guarda en el momento del registro (no se recalcula despues) para que un
/// recibo emitido sea reproducible aunque cambie el historial de medidores.
/// </summary>
public class Medicion
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int ServicioId { get; set; }
    public int MedidorId { get; set; }
    public int PeriodoId { get; set; }

    /// <summary>Fecha real en que el lector tomo la lectura.</summary>
    public DateOnly FechaLectura { get; set; }

    public decimal Lectura { get; set; }
    public decimal Consumo { get; set; }

    /// <summary>True si un operador debe revisarla (lectura tardia, periodo anterior sin lectura, consumo anormal).</summary>
    public bool RequiereRevision { get; set; }

    /// <summary>Motivos de la revision, separados por " | ".</summary>
    public string? MotivosRevision { get; set; }

    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

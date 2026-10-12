using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Recibo mensual de un servicio. Todo lo que se imprime en el recibo se guarda aqui en el
/// momento de emitirlo (nombre del abonado, direccion, lecturas, consumo, tarifa y desglose):
/// si despues cambia el expediente, la tarifa o el historial de medidores, el recibo emitido
/// sigue diciendo exactamente lo mismo (correccion C-02 del SRS: valores congelados).
/// </summary>
public class Recibo
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int ServicioId { get; set; }
    public int PeriodoId { get; set; }
    public int MedicionId { get; set; }
    public int TarifaId { get; set; }

    /// <summary>Correlativo por organizacion y anio del periodo.</summary>
    public int Correlativo { get; set; }

    /// <summary>Numero visible, p. ej. "CB-REC-2026-000125".</summary>
    public string Numero { get; set; } = "";

    public EstadoRecibo Estado { get; set; } = EstadoRecibo.Emitido;
    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    // Datos congelados del servicio al momento de emitir.
    public required string AbonadoCodigo { get; set; }
    public required string AbonadoNombre { get; set; }
    public required string PropiedadDireccion { get; set; }
    public required string CategoriaNombre { get; set; }
    public required string NumeroSerieMedidor { get; set; }

    /// <summary>Lectura del periodo anterior (null en la primera lectura del servicio).</summary>
    public decimal? LecturaAnterior { get; set; }
    public decimal LecturaActual { get; set; }
    public DateOnly FechaLectura { get; set; }
    public decimal Consumo { get; set; }

    /// <summary>True si entre la lectura anterior y la actual hubo cambio de medidor (RB-013):
    /// en ese caso el consumo no es la resta directa de las dos lecturas.</summary>
    public bool HuboCambioMedidor { get; set; }

    /// <summary>Copiados de la lectura: el recibo se emite igual, pero queda senalado.</summary>
    public bool LecturaRequiereRevision { get; set; }
    public string? MotivosRevision { get; set; }

    public decimal SubtotalAgua { get; set; }
    public decimal MontoHidrante { get; set; }
    public decimal Total { get; set; }

    public List<ReciboDetalle> Detalles { get; set; } = [];

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

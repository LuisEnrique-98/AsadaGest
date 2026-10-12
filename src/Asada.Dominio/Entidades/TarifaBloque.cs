namespace Asada.Dominio.Entidades;

/// <summary>
/// Tramo de consumo de una tarifa. Los tramos son consecutivos: cada uno empieza donde termina
/// el anterior (el primero empieza en 0 m³) y el ultimo no tiene limite (LimiteSuperior = null).
/// Cada m³ se cobra al precio del tramo en que cae (tarifa escalonada).
/// </summary>
public class TarifaBloque
{
    public int Id { get; set; }
    public int TarifaId { get; set; }

    /// <summary>Posicion del tramo dentro de la tarifa (1, 2, 3...).</summary>
    public int Orden { get; set; }

    /// <summary>Hasta cuantos m³ llega este tramo (inclusive), o null si es el ultimo tramo.</summary>
    public decimal? LimiteSuperior { get; set; }

    public decimal PrecioPorMetroCubico { get; set; }
}

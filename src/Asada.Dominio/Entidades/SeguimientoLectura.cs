using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Anotacion del operador sobre un servicio al que no se le tomo lectura en un periodo.
/// Hay a lo sumo una por servicio y periodo.
/// </summary>
public class SeguimientoLectura
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int ServicioId { get; set; }
    public int PeriodoId { get; set; }

    public DecisionSeguimientoLectura Decision { get; set; }
    public string? Nota { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}

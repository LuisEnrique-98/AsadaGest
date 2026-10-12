using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Conexion de agua de una propiedad. La morosidad vive aqui, no en Abonado (correccion C-01
/// del SRS v1.1): los recibos se generan por servicio, y un abonado con dos servicios puede
/// estar al dia en uno y moroso en el otro. En Etapa 1 estos datos se cargan manualmente.
/// </summary>
public class Servicio
{
    public int Id { get; set; }

    /// <summary>Se repite aqui (y en Abonado/Propiedad) para que el filtro multiorganizacion
    /// funcione con una sola comparacion, sin joins.</summary>
    public int OrganizacionId { get; set; }

    public int PropiedadId { get; set; }
    public Propiedad? Propiedad { get; set; }

    public DateOnly FechaInicio { get; set; }
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    public string? Observaciones { get; set; }

    public bool EsMoroso { get; set; }
    public int MensualidadesPendientes { get; set; }
    public decimal MontoPendiente { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

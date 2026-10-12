using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Categoria tarifaria de un servicio (por ejemplo "Domiciliar", "Empresarial", "Preferencial").
/// Cada ASADA define las suyas: los nombres y la cantidad dependen de su pliego tarifario, por
/// eso es una tabla y no un enum. Cada categoria tiene su propio historial de tarifas.
/// </summary>
public class CategoriaServicio
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

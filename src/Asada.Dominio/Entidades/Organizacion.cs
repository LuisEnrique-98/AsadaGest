using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Una ASADA dada de alta en la plataforma. Es la entidad raiz de la multiorganizacion:
/// toda la informacion operativa (abonados, mediciones, tarifas, recibos...) pertenece
/// a exactamente una Organizacion, salvo el Superadministrador, que no pertenece a ninguna.
///
/// Esta entidad vive en Asada.Dominio porque es un concepto de negocio de primer nivel
/// (aparece explicitamente en el SRS, &#167;7 y &#167;22.1), no un detalle de infraestructura.
/// No tiene ninguna dependencia externa: ni de Entity Framework, ni de ASP.NET Core Identity.
/// </summary>
public class Organizacion
{
    public int Id { get; set; }

    /// <summary>Prefijo corto usado en identificadores compuestos, p. ej. "CB" para Cuatro Bocas.</summary>
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public string? CedulaJuridica { get; set; }

    public string? Provincia { get; set; }
    public string? Canton { get; set; }
    public string? Distrito { get; set; }
    public string? Direccion { get; set; }

    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? LogoUrl { get; set; }

    public EstadoOrganizacion Estado { get; set; } = EstadoOrganizacion.Activa;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Ultimo correlativo asignado a un abonado de esta organizacion. Se incrementa de
    /// forma atomica en la base de datos al crear cada abonado (ver RepositorioAbonados).</summary>
    public int UltimoCorrelativoAbonado { get; set; }

    /// <summary>True si la organizacion puede operar (usuarios pueden iniciar sesion, etc.).</summary>
    public bool EstaActiva => Estado == EstadoOrganizacion.Activa;
}

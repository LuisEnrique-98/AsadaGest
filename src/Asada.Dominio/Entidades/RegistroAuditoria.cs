using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Una fila de la bitacora de auditoria: quien cambio que registro, cuando y como (valores antes y
/// despues de cada campo). La escribe automaticamente la capa de datos cada vez que se guarda un
/// cambio en una entidad del negocio; ningun caso de uso la escribe a mano. Nunca se modifica ni
/// se borra.
/// </summary>
public class RegistroAuditoria
{
    public long Id { get; set; }

    /// <summary>ASADA a la que pertenece el registro cambiado (null solo en cambios de la plataforma).</summary>
    public int? OrganizacionId { get; set; }

    /// <summary>Usuario que hizo el cambio (null si lo hizo el sistema, p. ej. los datos iniciales).</summary>
    public int? UsuarioId { get; set; }

    /// <summary>Nombre del usuario en el momento del cambio (se guarda por si despues cambia o se desactiva).</summary>
    public required string UsuarioNombre { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public AccionAuditoria Accion { get; set; }

    /// <summary>Nombre de la entidad (Recibo, Medicion, Abonado...).</summary>
    public required string Entidad { get; set; }
    public long EntidadId { get; set; }

    /// <summary>Campos cambiados en JSON: [{"Campo":"Lectura","Antes":"120","Despues":"125"}, ...].</summary>
    public required string Cambios { get; set; }
}

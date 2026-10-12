using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Persona (fisica o juridica) que recibe el servicio. Es la raiz del agregado
/// Abonado -> Propiedades -> Servicios: las propiedades y servicios se crean siempre a
/// traves de este objeto para que hereden la organizacion correcta.
/// </summary>
public class Abonado
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public Organizacion? Organizacion { get; set; }

    /// <summary>Numero consecutivo dentro de la organizacion (independiente entre ASADAs).</summary>
    public int Correlativo { get; set; }

    /// <summary>Identificador visible, p. ej. "CB-AB-000125".</summary>
    public string Codigo { get; set; } = "";

    public TipoIdentificacion TipoIdentificacion { get; set; }

    /// <summary>Siempre normalizada (ver ReglasIdentificacion.Normalizar).</summary>
    public required string Identificacion { get; set; }

    public required string Nombre { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public List<Propiedad> Propiedades { get; set; } = [];

    public static string GenerarCodigo(string codigoOrganizacion, int correlativo)
        => $"{codigoOrganizacion}-AB-{correlativo:D6}";

    /// <summary>Actualiza los datos editables. El codigo y el correlativo nunca cambian.</summary>
    public void ActualizarDatos(
        TipoIdentificacion tipo, string identificacionNormalizada, string nombre,
        string? telefono, string? correo, string? direccion)
    {
        TipoIdentificacion = tipo;
        Identificacion = identificacionNormalizada;
        Nombre = nombre;
        Telefono = telefono;
        Correo = correo;
        Direccion = direccion;
    }

    /// <summary>Un abonado inactivo se conserva con todo su historial; no se elimina.</summary>
    public void CambiarEstado(EstadoRegistro estado) => Estado = estado;

    public Propiedad AgregarPropiedad(
        string direccion, string? provincia = null, string? canton = null,
        string? distrito = null, string? referencia = null)
    {
        var propiedad = new Propiedad
        {
            OrganizacionId = OrganizacionId,
            Abonado = this,
            Direccion = direccion,
            Provincia = provincia,
            Canton = canton,
            Distrito = distrito,
            Referencia = referencia,
        };
        Propiedades.Add(propiedad);
        return propiedad;
    }
}

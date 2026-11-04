namespace Asada.Aplicacion.Common;

/// <summary>
/// Usuario autenticado de la solicitud actual: quien registra una lectura, genera o anula un
/// recibo, cierra un periodo... (Sprint 5). La implementacion lee los claims del usuario en
/// Asada.Infraestructura; en las pruebas se usa un valor fijo.
/// </summary>
public interface IProveedorUsuario
{
    /// <summary>Id del usuario, o null si no hay usuario autenticado (p. ej. los datos iniciales).</summary>
    int? UsuarioId { get; }

    /// <summary>Nombre para mostrar ("Sistema" si no hay usuario autenticado).</summary>
    string Nombre { get; }
}

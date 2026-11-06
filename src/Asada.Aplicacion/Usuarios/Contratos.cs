using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Usuarios;

/// <summary>
/// Nombres de los roles de la plataforma (SRS &#167;23). Viven aqui, en Aplicacion, porque los casos de
/// uso de usuarios los necesitan; Asada.Infraestructura (NombresRol) los reutiliza para no repetirlos.
/// </summary>
public static class RolesUsuario
{
    public const string Superadministrador = "Superadministrador";
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Consulta = "Consulta";

    /// <summary>Roles que se asignan dentro de una ASADA, de mas a menos permisos.</summary>
    public static readonly IReadOnlyList<string> DeAsada = [Administrador, Operador, Consulta];

    public static bool EsRolDeAsada(string? rol) => rol is not null && DeAsada.Contains(rol);
}

/// <summary>
/// Un usuario de una ASADA, tal como se muestra en la lista y en su ficha. BloqueadoHasta (UTC) solo
/// trae valor si el usuario esta bloqueado ahora por intentos fallidos; un bloqueo ya vencido llega null.
/// </summary>
public sealed record UsuarioDto(
    int Id,
    int? OrganizacionId,
    string Nombre,
    string Correo,
    string Rol,
    EstadoUsuario Estado,
    DateTimeOffset? BloqueadoHasta,
    int IntentosFallidos,
    bool DebeCambiarContrasena,
    DateTime? UltimoAcceso,
    DateTime FechaCreacion)
{
    public bool EstaActivo => Estado == EstadoUsuario.Activo;
    public bool EstaBloqueado => BloqueadoHasta is not null;
}

/// <summary>Datos para crear un usuario; la contrasena es la temporal que genero el sistema.</summary>
public sealed record DatosNuevoUsuario(int OrganizacionId, string Nombre, string Correo, string Rol, string ContrasenaTemporal);

/// <summary>
/// Lo que se anota en la bitacora junto con un cambio de usuario. Lo arma el caso de uso (sabe que
/// cambio y por que) y lo escribe el gestor en la misma transaccion que el cambio.
/// </summary>
public sealed record RegistroBitacora(AccionAuditoria Accion, IReadOnlyList<CambioAuditoria> Cambios);

/// <summary>
/// Acceso a los usuarios de la plataforma. La implementacion (Asada.Infraestructura) usa ASP.NET Core
/// Identity: hash de contrasenas, bloqueo por intentos, sellos de seguridad.
///
/// La tabla de usuarios NO tiene el filtro global por organizacion (el inicio de sesion la consulta
/// antes de saber quien es el usuario), asi que cada metodo recibe la organizacion de forma explicita
/// y solo ve o cambia usuarios de esa organizacion. El caso de uso decide que organizacion le
/// corresponde a quien hace la solicitud.
///
/// Los metodos que cambian algo invalidan las sesiones abiertas del usuario cuando hace falta
/// (desactivar, cambiar rol, restablecer contrasena): el usuario afectado sale del sistema en
/// menos de un minuto o recibe sus permisos nuevos.
/// </summary>
public interface IGestorUsuarios
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(int organizacionId, CancellationToken ct = default);
    Task<UsuarioDto?> ObtenerAsync(int organizacionId, int usuarioId, CancellationToken ct = default);

    /// <summary>True si el correo ya lo usa otro usuario, de cualquier ASADA (el correo es el usuario de acceso).</summary>
    Task<bool> ExisteCorreoAsync(string correo, int? excluirUsuarioId = null, CancellationToken ct = default);

    Task<int> ContarAdministradoresActivosAsync(int organizacionId, CancellationToken ct = default);

    Task<Resultado<int>> CrearAsync(DatosNuevoUsuario datos, RegistroBitacora bitacora, CancellationToken ct = default);
    Task<Resultado> ActualizarAsync(int organizacionId, int usuarioId, string nombre, string correo, string rol, RegistroBitacora bitacora, CancellationToken ct = default);
    Task<Resultado> CambiarEstadoAsync(int organizacionId, int usuarioId, EstadoUsuario estado, RegistroBitacora bitacora, CancellationToken ct = default);

    /// <summary>Reemplaza la contrasena por una temporal y obliga a cambiarla al entrar.</summary>
    Task<Resultado> RestablecerContrasenaAsync(int organizacionId, int usuarioId, string contrasenaTemporal, RegistroBitacora bitacora, CancellationToken ct = default);

    /// <summary>Quita el bloqueo por intentos fallidos y reinicia el contador.</summary>
    Task<Resultado> DesbloquearAsync(int organizacionId, int usuarioId, RegistroBitacora bitacora, CancellationToken ct = default);

    /// <summary>
    /// El usuario cambia su propia contrasena (se verifica la actual). Quita la marca de contrasena
    /// temporal. Falla con un mensaje claro si la actual no es correcta.
    /// </summary>
    Task<Resultado> CambiarContrasenaPropiaAsync(int usuarioId, string contrasenaActual, string contrasenaNueva, RegistroBitacora bitacora, CancellationToken ct = default);

    /// <summary>Datos del usuario que esta usando el sistema (cualquier organizacion, o el Superadministrador).</summary>
    Task<UsuarioDto?> ObtenerPropioAsync(int usuarioId, CancellationToken ct = default);
}

/// <summary>Genera las contrasenas temporales. Interfaz para poder fijarlas en las pruebas.</summary>
public interface IGeneradorContrasena
{
    string GenerarTemporal();
}

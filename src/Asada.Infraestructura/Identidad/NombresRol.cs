using Asada.Aplicacion.Usuarios;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Nombres de rol reconocidos por la plataforma (SRS &#167;23, matriz de permisos). Desde el Sprint 5b
/// son los mismos de Asada.Aplicacion.Usuarios.RolesUsuario (los casos de uso de usuarios los
/// necesitan); aqui se conservan para no cambiar el codigo que ya los usaba.
/// </summary>
public static class NombresRol
{
    public const string Superadministrador = RolesUsuario.Superadministrador;
    public const string Administrador = RolesUsuario.Administrador;
    public const string Operador = RolesUsuario.Operador;
    public const string Consulta = RolesUsuario.Consulta;

    public static readonly IReadOnlyList<string> Todos =
    [
        Superadministrador,
        Administrador,
        Operador,
        Consulta,
    ];
}

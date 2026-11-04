namespace Asada.Dominio.Enums;

/// <summary>Tipo de cambio registrado en la bitacora de auditoria.</summary>
public enum AccionAuditoria
{
    Creacion = 1,
    Modificacion = 2,
    Eliminacion = 3,

    // Sprint 5b: accesos al sistema (los anota el inicio de sesion, no el interceptor).
    InicioSesion = 4,
    InicioSesionFallido = 5,
    CuentaBloqueada = 6,
}

using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Organizaciones;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Usuarios;

// Sprint 5b -- gestion de usuarios de una ASADA (decisiones del 4 de noviembre de 2026):
//  - El Administrador de la ASADA crea, edita, desactiva y reactiva usuarios de SU ASADA, les
//    restablece la contrasena y los desbloquea. El Superadministrador puede hacer lo mismo en
//    cualquier ASADA (por ejemplo, cuando el unico Administrador olvido su contrasena).
//  - Al crear un usuario o restablecer su contrasena, el sistema genera una contrasena temporal que
//    se muestra una sola vez; el usuario debe cambiarla al entrar.
//  - Ninguna ASADA se queda sin un Administrador activo, y nadie se desactiva ni se cambia el rol a
//    si mismo (asi nadie se deja fuera por accidente).
//  - Cada accion queda en la bitacora de auditoria (entidad "Usuario").

/// <summary>Validaciones y textos de bitacora compartidos por los casos de uso de usuarios.</summary>
internal static class ValidacionUsuarios
{
    public const int LargoMaximoNombre = 150;
    public const int LargoMaximoCorreo = 256;

    public sealed record DatosLimpios(string Nombre, string Correo);

    /// <summary>Valida nombre y correo; devuelve los valores limpios (correo en minusculas).</summary>
    public static DatosLimpios Validar(List<string> errores, string? nombre, string? correo, string sufijo = "")
    {
        var de = sufijo.Length == 0 ? "" : " " + sufijo;
        Validacion.Requerido(errores, nombre, LargoMaximoNombre, $"El nombre{de}");
        var correoLimpio = (correo ?? "").Trim().ToLowerInvariant();
        if (correoLimpio.Length == 0) errores.Add($"El correo{de} es obligatorio.");
        else if (!Validacion.CorreoValido(correoLimpio) || correoLimpio.Length > LargoMaximoCorreo)
            errores.Add($"El correo{de} no es valido.");
        return new DatosLimpios((nombre ?? "").Trim(), correoLimpio);
    }

    /// <summary>
    /// A que ASADA se aplica la accion: la propia para un Administrador; la que eligio el
    /// Superadministrador (que no tiene ASADA propia).
    /// </summary>
    public static Resultado<int> Organizacion(IProveedorOrganizacion proveedor, int? solicitada)
    {
        if (proveedor.EsSuperadministrador)
            return solicitada is int elegida
                ? Resultado<int>.Ok(elegida)
                : Resultado<int>.Falla("Elija primero la ASADA (Plataforma > ASADAs).");
        if (proveedor.OrganizacionId is not int propia)
            return Resultado<int>.Falla("Su usuario no pertenece a ninguna ASADA.");
        if (solicitada is int otra && otra != propia)
            return Resultado<int>.Falla("No tiene acceso a los usuarios de otra ASADA.");
        return Resultado<int>.Ok(propia);
    }

    public static RegistroBitacora BitacoraCreacion(string nombre, string correo, string rol) => new(
        AccionAuditoria.Creacion,
        [
            new CambioAuditoria("Nombre", null, nombre),
            new CambioAuditoria("Correo", null, correo),
            new CambioAuditoria("Rol", null, rol),
            new CambioAuditoria("Estado", null, nameof(EstadoUsuario.Activo)),
            new CambioAuditoria("Contrasena", null, "Temporal generada por el sistema"),
        ]);

    public static RegistroBitacora Modificacion(params CambioAuditoria[] cambios) => new(AccionAuditoria.Modificacion, cambios);

    public const string MensajeUltimoAdministrador =
        "La ASADA debe tener al menos un Administrador activo. Asigne primero el rol Administrador a otro usuario.";
}

/// <summary>Usuarios de una ASADA, con su nombre (para el encabezado de la pagina).</summary>
public sealed record UsuariosAsadaDto(int OrganizacionId, string CodigoAsada, string NombreAsada, IReadOnlyList<UsuarioDto> Usuarios);

public class ListarUsuariosCasoUso(IGestorUsuarios gestor, IRepositorioOrganizaciones organizaciones, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<UsuariosAsadaDto>> EjecutarAsync(int? organizacionId = null, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, organizacionId);
        if (!alcance.EsExitoso) return Resultado<UsuariosAsadaDto>.Falla(alcance.Errores);

        var asada = await organizaciones.ObtenerAsync(alcance.Valor, ct);
        if (asada is null) return Resultado<UsuariosAsadaDto>.Falla("La ASADA no existe.");

        var usuarios = await gestor.ListarAsync(alcance.Valor, ct);
        return Resultado<UsuariosAsadaDto>.Ok(new UsuariosAsadaDto(asada.Id, asada.Codigo, asada.Nombre, usuarios));
    }
}

public class ObtenerUsuarioCasoUso(IGestorUsuarios gestor, IProveedorOrganizacion proveedorOrganizacion, IProveedorUsuario proveedorUsuario)
{
    public sealed record UsuarioFichaDto(UsuarioDto Usuario, bool EsUstedMismo);

    public async Task<Resultado<UsuarioFichaDto>> EjecutarAsync(int usuarioId, int? organizacionId = null, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, organizacionId);
        if (!alcance.EsExitoso) return Resultado<UsuarioFichaDto>.Falla(alcance.Errores);

        var usuario = await gestor.ObtenerAsync(alcance.Valor, usuarioId, ct);
        return usuario is null
            ? Resultado<UsuarioFichaDto>.Falla("El usuario no existe.")
            : Resultado<UsuarioFichaDto>.Ok(new UsuarioFichaDto(usuario, usuario.Id == proveedorUsuario.UsuarioId));
    }
}

public sealed record CrearUsuarioSolicitud(int? OrganizacionId, string? Nombre, string? Correo, string? Rol);

/// <summary>Usuario creado y su contrasena temporal (se muestra una sola vez).</summary>
public sealed record UsuarioCreadoDto(int UsuarioId, string Nombre, string Correo, string Rol, string ContrasenaTemporal);

public class CrearUsuarioCasoUso(
    IGestorUsuarios gestor,
    IRepositorioOrganizaciones organizaciones,
    IGeneradorContrasena generador,
    IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<UsuarioCreadoDto>> EjecutarAsync(CrearUsuarioSolicitud s, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, s.OrganizacionId);
        if (!alcance.EsExitoso) return Resultado<UsuarioCreadoDto>.Falla(alcance.Errores);

        var errores = new List<string>();
        var datos = ValidacionUsuarios.Validar(errores, s.Nombre, s.Correo);
        if (!RolesUsuario.EsRolDeAsada(s.Rol)) errores.Add("Elija el rol del usuario.");
        if (errores.Count > 0) return Resultado<UsuarioCreadoDto>.Falla(errores);

        var asada = await organizaciones.ObtenerAsync(alcance.Valor, ct);
        if (asada is null) return Resultado<UsuarioCreadoDto>.Falla("La ASADA no existe.");
        if (!asada.EstaActiva) return Resultado<UsuarioCreadoDto>.Falla("La ASADA esta inactiva.");

        if (await gestor.ExisteCorreoAsync(datos.Correo, null, ct))
            return Resultado<UsuarioCreadoDto>.Falla("Ya existe un usuario con ese correo.");

        var temporal = generador.GenerarTemporal();
        var creado = await gestor.CrearAsync(
            new DatosNuevoUsuario(alcance.Valor, datos.Nombre, datos.Correo, s.Rol!, temporal),
            ValidacionUsuarios.BitacoraCreacion(datos.Nombre, datos.Correo, s.Rol!),
            ct);
        if (!creado.EsExitoso) return Resultado<UsuarioCreadoDto>.Falla(creado.Errores);

        return Resultado<UsuarioCreadoDto>.Ok(new UsuarioCreadoDto(creado.Valor, datos.Nombre, datos.Correo, s.Rol!, temporal));
    }
}

public sealed record ActualizarUsuarioSolicitud(int? OrganizacionId, int UsuarioId, string? Nombre, string? Correo, string? Rol);

/// <summary>Cambia nombre, correo (es el usuario de acceso) y rol. Cambiar el rol cierra las sesiones abiertas del usuario.</summary>
public class ActualizarUsuarioCasoUso(IGestorUsuarios gestor, IProveedorOrganizacion proveedorOrganizacion, IProveedorUsuario proveedorUsuario)
{
    public async Task<Resultado> EjecutarAsync(ActualizarUsuarioSolicitud s, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, s.OrganizacionId);
        if (!alcance.EsExitoso) return alcance;

        var errores = new List<string>();
        var datos = ValidacionUsuarios.Validar(errores, s.Nombre, s.Correo);
        if (!RolesUsuario.EsRolDeAsada(s.Rol)) errores.Add("Elija el rol del usuario.");
        if (errores.Count > 0) return Resultado.Falla(errores);

        var actual = await gestor.ObtenerAsync(alcance.Valor, s.UsuarioId, ct);
        if (actual is null) return Resultado.Falla("El usuario no existe.");

        var cambios = new List<CambioAuditoria>();
        if (actual.Nombre != datos.Nombre) cambios.Add(new CambioAuditoria("Nombre", actual.Nombre, datos.Nombre));
        if (!string.Equals(actual.Correo, datos.Correo, StringComparison.OrdinalIgnoreCase))
            cambios.Add(new CambioAuditoria("Correo", actual.Correo, datos.Correo));
        if (actual.Rol != s.Rol) cambios.Add(new CambioAuditoria("Rol", actual.Rol, s.Rol));
        if (cambios.Count == 0) return Resultado.Ok();

        if (actual.Rol != s.Rol)
        {
            if (actual.Id == proveedorUsuario.UsuarioId)
                return Resultado.Falla("No puede cambiar su propio rol. Pidaselo a otro Administrador.");
            if (actual.Rol == RolesUsuario.Administrador && actual.EstaActivo
                && await gestor.ContarAdministradoresActivosAsync(alcance.Valor, ct) <= 1)
                return Resultado.Falla(ValidacionUsuarios.MensajeUltimoAdministrador);
        }

        if (cambios.Any(c => c.Campo == "Correo") && await gestor.ExisteCorreoAsync(datos.Correo, actual.Id, ct))
            return Resultado.Falla("Ya existe un usuario con ese correo.");

        return await gestor.ActualizarAsync(alcance.Valor, actual.Id, datos.Nombre, datos.Correo, s.Rol!,
            ValidacionUsuarios.Modificacion([.. cambios]), ct);
    }
}

public sealed record CambiarEstadoUsuarioSolicitud(int? OrganizacionId, int UsuarioId, bool Activar);

/// <summary>Desactivar impide iniciar sesion (y cierra las sesiones abiertas); el usuario no se borra.</summary>
public class CambiarEstadoUsuarioCasoUso(IGestorUsuarios gestor, IProveedorOrganizacion proveedorOrganizacion, IProveedorUsuario proveedorUsuario)
{
    public async Task<Resultado> EjecutarAsync(CambiarEstadoUsuarioSolicitud s, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, s.OrganizacionId);
        if (!alcance.EsExitoso) return alcance;

        var actual = await gestor.ObtenerAsync(alcance.Valor, s.UsuarioId, ct);
        if (actual is null) return Resultado.Falla("El usuario no existe.");

        var nuevo = s.Activar ? EstadoUsuario.Activo : EstadoUsuario.Inactivo;
        if (actual.Estado == nuevo)
            return Resultado.Falla(s.Activar ? "El usuario ya esta activo." : "El usuario ya esta desactivado.");

        if (!s.Activar)
        {
            if (actual.Id == proveedorUsuario.UsuarioId)
                return Resultado.Falla("No puede desactivar su propio usuario.");
            if (actual.Rol == RolesUsuario.Administrador
                && await gestor.ContarAdministradoresActivosAsync(alcance.Valor, ct) <= 1)
                return Resultado.Falla(ValidacionUsuarios.MensajeUltimoAdministrador);
        }

        return await gestor.CambiarEstadoAsync(alcance.Valor, actual.Id, nuevo,
            ValidacionUsuarios.Modificacion(new CambioAuditoria("Estado", actual.Estado.ToString(), nuevo.ToString())), ct);
    }
}

public sealed record UsuarioObjetivoSolicitud(int? OrganizacionId, int UsuarioId);

/// <summary>
/// El Administrador restablece la contrasena de un usuario que la olvido: el sistema genera una
/// temporal (se muestra una vez), se cierran las sesiones abiertas del usuario y este debe cambiarla
/// al entrar. Tambien quita un bloqueo por intentos fallidos.
/// </summary>
public class RestablecerContrasenaCasoUso(
    IGestorUsuarios gestor,
    IGeneradorContrasena generador,
    IProveedorOrganizacion proveedorOrganizacion,
    IProveedorUsuario proveedorUsuario)
{
    public async Task<Resultado<string>> EjecutarAsync(UsuarioObjetivoSolicitud s, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, s.OrganizacionId);
        if (!alcance.EsExitoso) return Resultado<string>.Falla(alcance.Errores);

        var actual = await gestor.ObtenerAsync(alcance.Valor, s.UsuarioId, ct);
        if (actual is null) return Resultado<string>.Falla("El usuario no existe.");
        if (actual.Id == proveedorUsuario.UsuarioId)
            return Resultado<string>.Falla("Para cambiar su propia contrasena use la opcion Cambiar contrasena.");
        if (!actual.EstaActivo)
            return Resultado<string>.Falla("El usuario esta desactivado. Reactivelo primero.");

        var temporal = generador.GenerarTemporal();
        var cambios = new List<CambioAuditoria> { new("Contrasena", null, "Restablecida (temporal generada por el sistema)") };
        if (actual.EstaBloqueado) cambios.Add(new CambioAuditoria("Bloqueo", "Bloqueado", "Desbloqueado"));

        var resultado = await gestor.RestablecerContrasenaAsync(alcance.Valor, actual.Id, temporal,
            ValidacionUsuarios.Modificacion([.. cambios]), ct);
        return resultado.EsExitoso ? Resultado<string>.Ok(temporal) : Resultado<string>.Falla(resultado.Errores);
    }
}

/// <summary>Quita el bloqueo por intentos fallidos antes de los 15 minutos (el usuario conserva su contrasena).</summary>
public class DesbloquearUsuarioCasoUso(IGestorUsuarios gestor, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado> EjecutarAsync(UsuarioObjetivoSolicitud s, CancellationToken ct = default)
    {
        var alcance = ValidacionUsuarios.Organizacion(proveedorOrganizacion, s.OrganizacionId);
        if (!alcance.EsExitoso) return alcance;

        var actual = await gestor.ObtenerAsync(alcance.Valor, s.UsuarioId, ct);
        if (actual is null) return Resultado.Falla("El usuario no existe.");
        if (!actual.EstaBloqueado) return Resultado.Falla("El usuario no esta bloqueado.");

        return await gestor.DesbloquearAsync(alcance.Valor, actual.Id,
            ValidacionUsuarios.Modificacion(new CambioAuditoria("Bloqueo", "Bloqueado", "Desbloqueado")), ct);
    }
}

public sealed record CambiarContrasenaSolicitud(string? Actual, string? Nueva, string? Confirmacion);

/// <summary>Cualquier usuario cambia su propia contrasena; obligatorio cuando tiene una temporal.</summary>
public class CambiarContrasenaPropiaCasoUso(IGestorUsuarios gestor, IProveedorUsuario proveedorUsuario)
{
    public async Task<Resultado> EjecutarAsync(CambiarContrasenaSolicitud s, CancellationToken ct = default)
    {
        if (proveedorUsuario.UsuarioId is not int usuarioId)
            return Resultado.Falla("Inicie sesion para cambiar su contrasena.");

        var propio = await gestor.ObtenerPropioAsync(usuarioId, ct);
        if (propio is null) return Resultado.Falla("Su usuario no existe.");

        var errores = new List<string>();
        if (string.IsNullOrEmpty(s.Actual)) errores.Add("Indique su contrasena actual.");
        errores.AddRange(ReglasContrasena.Validar(s.Nueva, propio.Correo).Select(e => e.Replace("La contrasena", "La contrasena nueva")));
        if (s.Nueva is not null && s.Confirmacion != s.Nueva) errores.Add("La confirmacion no coincide con la contrasena nueva.");
        if (!string.IsNullOrEmpty(s.Actual) && s.Nueva == s.Actual) errores.Add("La contrasena nueva debe ser distinta de la actual.");
        if (errores.Count > 0) return Resultado.Falla(errores);

        return await gestor.CambiarContrasenaPropiaAsync(usuarioId, s.Actual!, s.Nueva!,
            ValidacionUsuarios.Modificacion(new CambioAuditoria("Contrasena", null,
                propio.DebeCambiarContrasena ? "Cambiada por el usuario (reemplaza la temporal)" : "Cambiada por el usuario")), ct);
    }
}

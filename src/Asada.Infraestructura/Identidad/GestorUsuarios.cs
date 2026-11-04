using System.Linq.Expressions;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Usuarios;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Infraestructura.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Implementacion de IGestorUsuarios sobre ASP.NET Core Identity (Sprint 5b).
///
/// Aislamiento: la tabla usuarios no tiene HasQueryFilter (el inicio de sesion la consulta antes de
/// saber quien es el usuario), asi que AQUI cada consulta filtra explicitamente por la organizacion
/// que recibe. El caso de uso decide cual le corresponde a quien hace la solicitud.
///
/// Cada cambio y su fila de auditoria se guardan en una misma transaccion: o quedan los dos o
/// ninguno. Al desactivar, cambiar el rol o el correo, o restablecer la contrasena se cambia el sello
/// de seguridad del usuario; sus sesiones abiertas se revalidan cada minuto (ver
/// SecurityStampValidatorOptions) y con un sello distinto se cierran.
/// </summary>
public sealed class GestorUsuarios(UserManager<Usuario> userManager, AsadaDbContext db, IProveedorUsuario proveedorUsuario)
    : IGestorUsuarios
{
    // ---- Consultas ----

    private sealed record Fila(
        int Id, int? OrganizacionId, string Nombre, string? Correo, string? Rol, EstadoUsuario Estado,
        DateTimeOffset? LockoutEnd, int AccessFailedCount, bool DebeCambiarContrasena, DateTime? UltimoAcceso, DateTime FechaCreacion);

    // El filtro va ANTES de proyectar: EF Core no traduce un Where sobre un objeto creado con constructor.
    private IQueryable<Fila> Consulta(Expression<Func<Usuario, bool>> filtro) =>
        from u in db.Users.Where(filtro).OrderBy(u => u.Nombre)
        select new Fila(
            u.Id, u.OrganizacionId, u.Nombre, u.Email,
            (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == u.Id select r.Name).FirstOrDefault(),
            u.Estado, u.LockoutEnd, u.AccessFailedCount, u.DebeCambiarContrasena, u.UltimoAcceso, u.FechaCreacion);

    private static UsuarioDto ADto(Fila f)
    {
        var bloqueadoHasta = f.LockoutEnd is DateTimeOffset fin && fin > DateTimeOffset.UtcNow ? fin : (DateTimeOffset?)null;
        return new UsuarioDto(f.Id, f.OrganizacionId, f.Nombre, f.Correo ?? "", f.Rol ?? "", f.Estado, bloqueadoHasta,
            f.AccessFailedCount, f.DebeCambiarContrasena, f.UltimoAcceso, f.FechaCreacion);
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(int organizacionId, CancellationToken ct = default)
    {
        var filas = await Consulta(u => u.OrganizacionId == organizacionId).ToListAsync(ct);
        return filas.Select(ADto).ToList();
    }

    public async Task<UsuarioDto?> ObtenerAsync(int organizacionId, int usuarioId, CancellationToken ct = default)
    {
        var fila = await Consulta(u => u.Id == usuarioId && u.OrganizacionId == organizacionId).FirstOrDefaultAsync(ct);
        return fila is null ? null : ADto(fila);
    }

    public async Task<UsuarioDto?> ObtenerPropioAsync(int usuarioId, CancellationToken ct = default)
    {
        var fila = await Consulta(u => u.Id == usuarioId).FirstOrDefaultAsync(ct);
        return fila is null ? null : ADto(fila);
    }

    public Task<bool> ExisteCorreoAsync(string correo, int? excluirUsuarioId = null, CancellationToken ct = default)
    {
        var normalizado = userManager.NormalizeEmail(correo);
        return db.Users.AnyAsync(u => u.Id != excluirUsuarioId
            && (u.NormalizedEmail == normalizado || u.NormalizedUserName == normalizado), ct);
    }

    public async Task<int> ContarAdministradoresActivosAsync(int organizacionId, CancellationToken ct = default)
    {
        var rolId = await db.Roles.Where(r => r.Name == NombresRol.Administrador).Select(r => r.Id).FirstOrDefaultAsync(ct);
        return await db.Users.CountAsync(u => u.OrganizacionId == organizacionId
            && u.Estado == EstadoUsuario.Activo
            && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == rolId), ct);
    }

    // ---- Cambios ----

    public async Task<Resultado<int>> CrearAsync(DatosNuevoUsuario datos, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var usuario = new Usuario
        {
            UserName = datos.Correo,
            Email = datos.Correo,
            EmailConfirmed = true,
            Nombre = datos.Nombre,
            OrganizacionId = datos.OrganizacionId,
            Estado = EstadoUsuario.Activo,
            DebeCambiarContrasena = true,
        };

        var resultado = await EnTransaccionAsync(usuario, bitacora, async () =>
        {
            var creado = await userManager.CreateAsync(usuario, datos.ContrasenaTemporal);
            return creado.Succeeded ? await userManager.AddToRoleAsync(usuario, datos.Rol) : creado;
        }, ct);

        return resultado.EsExitoso ? Resultado<int>.Ok(usuario.Id) : Resultado<int>.Falla(resultado.Errores);
    }

    public async Task<Resultado> ActualizarAsync(int organizacionId, int usuarioId, string nombre, string correo, string rol,
        RegistroBitacora bitacora, CancellationToken ct = default)
    {
        if (await BuscarAsync(organizacionId, usuarioId, ct) is not { } usuario) return Resultado.Falla("El usuario no existe.");

        return await EnTransaccionAsync(usuario, bitacora, async () =>
        {
            usuario.Nombre = nombre;

            if (!string.Equals(usuario.Email, correo, StringComparison.OrdinalIgnoreCase))
            {
                // El correo es tambien el usuario de acceso. SetEmailAsync cambia el sello de seguridad
                // y marca el correo como no confirmado; aqui no se usa confirmacion por correo.
                var r = await userManager.SetEmailAsync(usuario, correo);
                if (!r.Succeeded) return r;
                r = await userManager.SetUserNameAsync(usuario, correo);
                if (!r.Succeeded) return r;
                usuario.EmailConfirmed = true;
            }

            var rolesActuales = await userManager.GetRolesAsync(usuario);
            if (!(rolesActuales.Count == 1 && rolesActuales[0] == rol))
            {
                var r = await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                if (!r.Succeeded) return r;
                r = await userManager.AddToRoleAsync(usuario, rol);
                if (!r.Succeeded) return r;
                return await userManager.UpdateSecurityStampAsync(usuario);
            }

            return await userManager.UpdateAsync(usuario);
        }, ct);
    }

    public async Task<Resultado> CambiarEstadoAsync(int organizacionId, int usuarioId, EstadoUsuario estado,
        RegistroBitacora bitacora, CancellationToken ct = default)
    {
        if (await BuscarAsync(organizacionId, usuarioId, ct) is not { } usuario) return Resultado.Falla("El usuario no existe.");

        return await EnTransaccionAsync(usuario, bitacora, () =>
        {
            usuario.Estado = estado;
            // Guarda el estado y cambia el sello: las sesiones abiertas se cierran en menos de un minuto.
            return userManager.UpdateSecurityStampAsync(usuario);
        }, ct);
    }

    public async Task<Resultado> RestablecerContrasenaAsync(int organizacionId, int usuarioId, string contrasenaTemporal,
        RegistroBitacora bitacora, CancellationToken ct = default)
    {
        if (await BuscarAsync(organizacionId, usuarioId, ct) is not { } usuario) return Resultado.Falla("El usuario no existe.");

        return await EnTransaccionAsync(usuario, bitacora, async () =>
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            var r = await userManager.ResetPasswordAsync(usuario, token, contrasenaTemporal); // cambia el sello
            if (!r.Succeeded) return r;

            usuario.DebeCambiarContrasena = true;
            usuario.LockoutEnd = null;
            usuario.AccessFailedCount = 0;
            return await userManager.UpdateAsync(usuario);
        }, ct);
    }

    public async Task<Resultado> DesbloquearAsync(int organizacionId, int usuarioId, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        if (await BuscarAsync(organizacionId, usuarioId, ct) is not { } usuario) return Resultado.Falla("El usuario no existe.");

        return await EnTransaccionAsync(usuario, bitacora, () =>
        {
            usuario.LockoutEnd = null;
            usuario.AccessFailedCount = 0;
            return userManager.UpdateAsync(usuario);
        }, ct);
    }

    public async Task<Resultado> CambiarContrasenaPropiaAsync(int usuarioId, string contrasenaActual, string contrasenaNueva,
        RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var usuario = await db.Users.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null) return Resultado.Falla("Su usuario no existe.");

        // Fuera de la transaccion: un intento con la contrasena actual equivocada cuenta para el
        // bloqueo, igual que en el inicio de sesion.
        if (!await userManager.CheckPasswordAsync(usuario, contrasenaActual))
        {
            await userManager.AccessFailedAsync(usuario);
            return Resultado.Falla("La contrasena actual no es correcta.");
        }

        return await EnTransaccionAsync(usuario, bitacora, async () =>
        {
            var r = await userManager.ChangePasswordAsync(usuario, contrasenaActual, contrasenaNueva); // cambia el sello
            if (!r.Succeeded) return r;
            usuario.DebeCambiarContrasena = false;
            usuario.AccessFailedCount = 0;
            return await userManager.UpdateAsync(usuario);
        }, ct);
    }

    // ---- Apoyo ----

    private Task<Usuario?> BuscarAsync(int organizacionId, int usuarioId, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Id == usuarioId && u.OrganizacionId == organizacionId, ct);

    /// <summary>
    /// Ejecuta las operaciones de Identity y, si todas salen bien, escribe la fila de auditoria; todo
    /// en una transaccion. UserManager guarda con el mismo AsadaDbContext (es Scoped), asi que sus
    /// SaveChanges quedan dentro de la transaccion.
    /// </summary>
    private async Task<Resultado> EnTransaccionAsync(Usuario usuario, RegistroBitacora bitacora,
        Func<Task<IdentityResult>> operacion, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var resultado = await operacion();
        if (!resultado.Succeeded)
        {
            await transaccion.RollbackAsync(ct);
            // El rollback deshace la base, pero no los objetos en memoria: el usuario seguiria con el
            // valor rechazado (p. ej. un correo repetido) y otro SaveChanges de la misma solicitud lo
            // guardaria. Se descarta todo lo que este contexto tenia cargado.
            db.ChangeTracker.Clear();
            return Resultado.Falla(resultado.Errors.Select(e => e.Description).Distinct());
        }

        db.Auditoria.Add(new RegistroAuditoria
        {
            OrganizacionId = usuario.OrganizacionId,
            UsuarioId = proveedorUsuario.UsuarioId,
            UsuarioNombre = proveedorUsuario.Nombre,
            Fecha = DateTime.UtcNow,
            Accion = bitacora.Accion,
            Entidad = "Usuario",
            EntidadId = usuario.Id,
            Cambios = CambioAuditoria.Serializar(bitacora.Cambios),
        });
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Resultado.Ok();
    }
}

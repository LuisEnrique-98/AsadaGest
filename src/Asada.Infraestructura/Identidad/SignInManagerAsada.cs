using Asada.Aplicacion.Auditoria;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Infraestructura.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// SignInManager de la plataforma (Sprint 5b). Agrega dos cosas al de ASP.NET Core Identity:
///
///  1. No deja entrar a un usuario desactivado ni a los usuarios de una ASADA inactiva
///     (CanSignInAsync; el inicio de sesion responde "NotAllowed").
///  2. Anota cada intento de inicio de sesion en la bitacora de auditoria: correcto, fallido (y por
///     que) y el momento en que la cuenta queda bloqueada por intentos. Un intento con un correo que
///     no existe no se anota (no hay usuario ni ASADA a la cual asignarlo); queda solo en el log.
///     Al entrar correctamente se actualiza tambien el ultimo acceso del usuario.
///
/// El bloqueo en si (5 intentos fallidos seguidos -> 15 minutos) lo hace Identity con las opciones
/// de InfraestructuraServiceCollectionExtensions.
/// </summary>
public sealed class SignInManagerAsada(
    UserManager<Usuario> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<Usuario> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<Usuario>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<Usuario> confirmation,
    AsadaDbContext db)
    : SignInManager<Usuario>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(Usuario user)
    {
        if (user.Estado != EstadoUsuario.Activo) return false;

        if (user.OrganizacionId is int organizacionId)
        {
            // Sin filtro de organizacion: todavia no hay usuario autenticado.
            var estado = await db.Organizaciones.IgnoreQueryFilters()
                .Where(o => o.Id == organizacionId)
                .Select(o => (EstadoOrganizacion?)o.Estado)
                .FirstOrDefaultAsync();
            if (estado != EstadoOrganizacion.Activa) return false;
        }

        return await base.CanSignInAsync(user);
    }

    public override async Task<SignInResult> CheckPasswordSignInAsync(Usuario user, string password, bool lockoutOnFailure)
    {
        var estabaBloqueado = await UserManager.IsLockedOutAsync(user);
        var resultado = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);

        try
        {
            await AnotarAsync(user, resultado, estabaBloqueado);
        }
        catch (Exception ex)
        {
            // La bitacora nunca debe impedir (ni permitir) un inicio de sesion.
            Logger.LogError(ex, "No se pudo anotar en la auditoria el intento de inicio de sesion del usuario {UsuarioId}.", user.Id);
        }

        return resultado;
    }

    private async Task AnotarAsync(Usuario user, SignInResult resultado, bool estabaBloqueado)
    {
        var ahora = DateTime.UtcNow;
        AccionAuditoria accion;
        var cambios = new List<CambioAuditoria>();

        if (resultado.Succeeded)
        {
            accion = AccionAuditoria.InicioSesion;
            user.UltimoAcceso = ahora;
        }
        else if (resultado.IsLockedOut && !estabaBloqueado)
        {
            accion = AccionAuditoria.CuentaBloqueada;
            cambios.Add(new CambioAuditoria("Motivo", null, $"{Options.Lockout.MaxFailedAccessAttempts} intentos fallidos seguidos"));
            if (user.LockoutEnd is DateTimeOffset hasta)
                cambios.Add(new CambioAuditoria("BloqueadoHasta", null, hasta.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"));
        }
        else
        {
            accion = AccionAuditoria.InicioSesionFallido;
            var motivo = resultado.IsLockedOut ? "La cuenta estaba bloqueada"
                : resultado.IsNotAllowed ? "Usuario desactivado o ASADA inactiva"
                : "Contrasena incorrecta";
            cambios.Add(new CambioAuditoria("Motivo", null, motivo));
            if (!resultado.IsLockedOut && !resultado.IsNotAllowed)
                cambios.Add(new CambioAuditoria("IntentosFallidos", null, $"{user.AccessFailedCount} de {Options.Lockout.MaxFailedAccessAttempts}"));
        }

        db.Auditoria.Add(new RegistroAuditoria
        {
            OrganizacionId = user.OrganizacionId,
            UsuarioId = user.Id,
            UsuarioNombre = user.Nombre,
            Fecha = ahora,
            Accion = accion,
            Entidad = "Usuario",
            EntidadId = user.Id,
            Cambios = CambioAuditoria.Serializar(cambios),
        });
        await db.SaveChangesAsync();
    }
}

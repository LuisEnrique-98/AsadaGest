using Asada.Aplicacion.Usuarios;
using Asada.Dominio.Enums;
using Asada.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Asada.Infraestructura.Persistence.Seed;

/// <summary>
/// Datos iniciales de un servidor real (Sprint 6a). A diferencia de <see cref="SeedInicial"/> (solo
/// Desarrollo), aqui NO se crea ninguna ASADA ni usuarios de prueba: solo los cuatro roles y el
/// Superadministrador de la plataforma, con el correo que indique la configuracion
/// (Plataforma:CorreoSuperadministrador) y una contrasena temporal generada por el sistema, que se
/// muestra una sola vez en la consola y se debe cambiar al entrar. Despues, el Superadministrador
/// registra Cuatro Bocas (y su primer Administrador) desde /plataforma/asadas/nueva.
///
/// Lo ejecutan los comandos de consola "migrar" y "restablecer-superadministrador" (ver
/// ComandosConsola en Asada.Presentacion). Las contrasenas nunca se guardan en archivos de
/// configuracion.
/// </summary>
public static class SeedProduccion
{
    public const string ClaveCorreoSuperadministrador = "Plataforma:CorreoSuperadministrador";

    public static async Task CrearRolesAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var roleManager = alcance.ServiceProvider.GetRequiredService<RoleManager<Rol>>();
        foreach (var nombreRol in NombresRol.Todos)
        {
            if (!await roleManager.RoleExistsAsync(nombreRol))
            {
                Verificar(await roleManager.CreateAsync(new Rol(nombreRol)), $"crear el rol {nombreRol}");
            }
        }
    }

    /// <summary>
    /// Crea el Superadministrador si no existe ningun usuario con ese correo. Devuelve la contrasena
    /// temporal, o null si el usuario ya existia (no se toca).
    /// </summary>
    public static async Task<string?> CrearSuperadministradorAsync(IServiceProvider servicios, string correo)
    {
        using var alcance = servicios.CreateScope();
        var userManager = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var db = alcance.ServiceProvider.GetRequiredService<AsadaDbContext>();
        var generador = alcance.ServiceProvider.GetRequiredService<IGeneradorContrasena>();

        correo = correo.Trim();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == correo)) return null;

        var temporal = generador.GenerarTemporal();
        var superadministrador = new Usuario
        {
            UserName = correo,
            Email = correo,
            EmailConfirmed = true,
            Nombre = "Superadministrador de la plataforma",
            OrganizacionId = null,
            DebeCambiarContrasena = true,
        };

        Verificar(await userManager.CreateAsync(superadministrador, temporal), "crear el Superadministrador");
        Verificar(await userManager.AddToRoleAsync(superadministrador, NombresRol.Superadministrador),
            "asignar el rol de Superadministrador");
        return temporal;
    }

    /// <summary>
    /// Para cuando se pierde la contrasena del Superadministrador (no hay recuperacion por correo):
    /// le genera una temporal nueva, lo desbloquea y lo reactiva. Solo actua sobre un usuario con el
    /// rol Superadministrador. El cambio de contrasena cambia el sello de seguridad, asi que cualquier
    /// sesion abierta con la contrasena anterior se cierra.
    /// </summary>
    public static async Task<string> RestablecerSuperadministradorAsync(IServiceProvider servicios, string correo)
    {
        using var alcance = servicios.CreateScope();
        var userManager = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var db = alcance.ServiceProvider.GetRequiredService<AsadaDbContext>();
        var generador = alcance.ServiceProvider.GetRequiredService<IGeneradorContrasena>();

        correo = correo.Trim();
        var usuario = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == correo)
            ?? throw new InvalidOperationException($"No existe ningun usuario con el correo {correo}.");
        if (!await userManager.IsInRoleAsync(usuario, NombresRol.Superadministrador))
        {
            throw new InvalidOperationException(
                $"El usuario {correo} no es Superadministrador. Las contrasenas de los usuarios de una ASADA " +
                "las restablece su Administrador (o el Superadministrador) desde la pagina Usuarios.");
        }

        var temporal = generador.GenerarTemporal();
        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        Verificar(await userManager.ResetPasswordAsync(usuario, token, temporal), "restablecer la contrasena");

        usuario.DebeCambiarContrasena = true;
        usuario.Estado = EstadoUsuario.Activo;
        usuario.LockoutEnd = null;
        usuario.AccessFailedCount = 0;
        Verificar(await userManager.UpdateAsync(usuario), "actualizar el Superadministrador");
        return temporal;
    }

    private static void Verificar(IdentityResult resultado, string accion)
    {
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo {accion}: " + string.Join("; ", resultado.Errors.Select(e => e.Description)));
        }
    }
}

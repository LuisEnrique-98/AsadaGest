using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Asada.Infraestructura.Persistence.Seed;

/// <summary>
/// Datos iniciales para poder arrancar a desarrollar: los cuatro roles de la plataforma
/// (SRS &#167;23), la organizacion Cuatro Bocas, y un usuario Administrador para poder iniciar
/// sesion por primera vez. Se llama desde Program.cs solo en entorno de Desarrollo.
///
/// Es seguro llamarlo en cada arranque: cada paso verifica primero si el dato ya existe
/// (usando IgnoreQueryFilters, porque durante el seed todavia no hay ningun usuario
/// autenticado y el filtro global de organizacion excluiria cualquier resultado).
/// </summary>
public static class SeedInicial
{
    public const string CodigoCuatroBocas = "CB";
    public const string CorreoAdministradorInicial = "admin@cuatrobocas.test";

    // Solo para desarrollo local. Antes de usar datos reales, este usuario debe cambiar su
    // contrasena (o eliminarse) -- ver la nota de seguridad en el README del repositorio.
    public const string ContrasenaAdministradorInicial = "CuatroBocas#2026";

    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var roleManager = alcance.ServiceProvider.GetRequiredService<RoleManager<Rol>>();
        var userManager = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var db = alcance.ServiceProvider.GetRequiredService<AsadaDbContext>();

        foreach (var nombreRol in NombresRol.Todos)
        {
            if (!await roleManager.RoleExistsAsync(nombreRol))
            {
                await roleManager.CreateAsync(new Rol(nombreRol));
            }
        }

        var cuatroBocas = await db.Organizaciones
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Codigo == CodigoCuatroBocas);

        if (cuatroBocas is null)
        {
            cuatroBocas = new Organizacion
            {
                Codigo = CodigoCuatroBocas,
                Nombre = "ASADA Cuatro Bocas",
                Estado = EstadoOrganizacion.Activa,
            };
            db.Organizaciones.Add(cuatroBocas);
            await db.SaveChangesAsync();
        }

        var yaExisteAdministrador = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == CorreoAdministradorInicial);

        if (!yaExisteAdministrador)
        {
            var administrador = new Usuario
            {
                UserName = CorreoAdministradorInicial,
                Email = CorreoAdministradorInicial,
                EmailConfirmed = true,
                Nombre = "Administrador Cuatro Bocas",
                OrganizacionId = cuatroBocas.Id,
                Estado = EstadoUsuario.Activo,
            };

            var resultado = await userManager.CreateAsync(administrador, ContrasenaAdministradorInicial);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el usuario administrador inicial: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(administrador, NombresRol.Administrador);
        }
    }
}

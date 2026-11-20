using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Asada.Infraestructura.Persistence.Seed;

/// <summary>
/// Datos iniciales para poder arrancar a desarrollar: los cuatro roles de la plataforma
/// (SRS &#167;23), la organizacion Cuatro Bocas, un usuario Administrador para poder iniciar
/// sesion por primera vez y (Sprint 5b) un Superadministrador de la plataforma, sin ASADA, que
/// registra las ASADAs nuevas. Se llama desde Program.cs solo en entorno de Desarrollo.
///
/// Es seguro llamarlo en cada arranque: cada paso verifica primero si el dato ya existe
/// (usando IgnoreQueryFilters, porque durante el seed todavia no hay ningun usuario
/// autenticado y el filtro global de organizacion excluiria cualquier resultado).
///
/// Sprint 6c (M-3): estas cuentas tienen contrasenas publicas (estan en el repositorio). Si un
/// servidor arrancara por error en Desarrollo, se crearian alli. Por eso el seed se niega a correr
/// si la base de datos no esta en la misma maquina (localhost, 127.0.0.1 o ::1).
/// </summary>
public static class SeedInicial
{
    public const string CodigoCuatroBocas = "CB";
    public const string CorreoAdministradorInicial = "admin@cuatrobocas.test";

    // Solo para desarrollo local. Antes de usar datos reales, este usuario debe cambiar su
    // contrasena (o eliminarse) -- ver la nota de seguridad en el README del repositorio.
    public const string ContrasenaAdministradorInicial = "CuatroBocas#2026";

    // Sprint 5b. Igual que el anterior: solo para desarrollo local.
    public const string CorreoSuperadministradorInicial = "superadmin@plataforma.test";
    public const string ContrasenaSuperadministradorInicial = "Plataforma#2026";

    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var roleManager = alcance.ServiceProvider.GetRequiredService<RoleManager<Rol>>();
        var userManager = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var db = alcance.ServiceProvider.GetRequiredService<AsadaDbContext>();

        if (!EsBaseLocal(db.Database.GetConnectionString()))
        {
            throw new InvalidOperationException(
                "Los datos de desarrollo (usuarios con contrasenas conocidas) solo se crean con una base de " +
                "datos local (localhost, 127.0.0.1 o ::1). Esta base esta en otro servidor: revise que " +
                "ASPNETCORE_ENVIRONMENT no sea Development fuera de la PC de desarrollo.");
        }

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

        var yaExisteSuperadministrador = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == CorreoSuperadministradorInicial);

        if (!yaExisteSuperadministrador)
        {
            var superadministrador = new Usuario
            {
                UserName = CorreoSuperadministradorInicial,
                Email = CorreoSuperadministradorInicial,
                EmailConfirmed = true,
                Nombre = "Superadministrador de la plataforma",
                OrganizacionId = null,
                Estado = EstadoUsuario.Activo,
            };

            var resultado = await userManager.CreateAsync(superadministrador, ContrasenaSuperadministradorInicial);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el Superadministrador inicial: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(superadministrador, NombresRol.Superadministrador);
        }
    }

    private static readonly string[] ServidoresLocales = ["localhost", "127.0.0.1", "::1", "[::1]"];

    /// <summary>
    /// True si todos los servidores de la cadena de conexion son la propia maquina. Acepta el
    /// puerto pegado al nombre ("localhost:5432"). Una cadena vacia o invalida no es local.
    /// </summary>
    public static bool EsBaseLocal(string? cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion)) return false;

        string? servidores;
        try
        {
            servidores = new NpgsqlConnectionStringBuilder(cadenaConexion).Host;
        }
        catch (ArgumentException)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(servidores)) return false;

        return servidores.Split(',').All(s => EsServidorLocal(s.Trim().ToLowerInvariant()));
    }

    private static bool EsServidorLocal(string servidor)
    {
        if (ServidoresLocales.Contains(servidor)) return true;

        var dosPuntos = servidor.LastIndexOf(':');
        return dosPuntos > 0
            && dosPuntos < servidor.Length - 1
            && servidor[(dosPuntos + 1)..].All(char.IsAsciiDigit)
            && ServidoresLocales.Contains(servidor[..dosPuntos]);
    }
}

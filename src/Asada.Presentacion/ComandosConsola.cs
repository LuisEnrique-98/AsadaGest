using Asada.Infraestructura.Persistence;
using Asada.Infraestructura.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Asada.Presentacion;

/// <summary>
/// Comandos de mantenimiento que se ejecutan con el mismo programa, en vez de levantar el sitio
/// (Sprint 6a). En el servidor:
///
///   docker compose run --rm app migrar
///   docker compose run --rm app restablecer-superadministrador
///
/// En la PC de desarrollo: dotnet run --project src/Asada.Presentacion -- migrar
///
/// - migrar: aplica las migraciones pendientes de EF Core, crea los roles y, si todavia no existe,
///   el Superadministrador (correo en Plataforma:CorreoSuperadministrador) con una contrasena temporal
///   que se muestra una sola vez. Es seguro repetirlo: no toca lo que ya existe. En Produccion la
///   aplicacion NO migra sola al arrancar; lo hace este comando (lo llaman instalar.sh y
///   actualizar.sh despues de un respaldo).
/// - restablecer-superadministrador: genera una contrasena temporal nueva para el Superadministrador
///   (por si se perdio; no hay recuperacion por correo).
/// </summary>
public static class ComandosConsola
{
    public const string Migrar = "migrar";
    public const string RestablecerSuperadministrador = "restablecer-superadministrador";

    public static bool EsComando(string argumento) =>
        argumento is Migrar or RestablecerSuperadministrador;

    public static async Task<int> EjecutarAsync(string comando, WebApplication app)
    {
        var correo = app.Configuration[SeedProduccion.ClaveCorreoSuperadministrador];
        try
        {
            if (comando == Migrar)
            {
                using (var alcance = app.Services.CreateScope())
                {
                    var db = alcance.ServiceProvider.GetRequiredService<AsadaDbContext>();
                    var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
                    Console.WriteLine(pendientes.Count == 0
                        ? "La base de datos ya esta al dia (no hay migraciones pendientes)."
                        : $"Aplicando {pendientes.Count} migracion(es): {string.Join(", ", pendientes)}");
                    await db.Database.MigrateAsync();
                }

                await SeedProduccion.CrearRolesAsync(app.Services);

                if (string.IsNullOrWhiteSpace(correo))
                {
                    Console.WriteLine("Aviso: no se indico Plataforma__CorreoSuperadministrador; no se creo el Superadministrador.");
                    return 0;
                }

                var temporal = await SeedProduccion.CrearSuperadministradorAsync(app.Services, correo);
                if (temporal is null)
                {
                    Console.WriteLine($"El Superadministrador {correo.Trim()} ya existe (no se modifico).");
                }
                else
                {
                    MostrarContrasena("Superadministrador creado", correo.Trim(), temporal);
                }

                return 0;
            }

            if (string.IsNullOrWhiteSpace(correo))
            {
                Console.Error.WriteLine("Falta Plataforma__CorreoSuperadministrador en la configuracion (.env del servidor).");
                return 1;
            }

            var nueva = await SeedProduccion.RestablecerSuperadministradorAsync(app.Services, correo);
            MostrarContrasena("Contrasena del Superadministrador restablecida", correo.Trim(), nueva);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR en el comando '{comando}': {ex.Message}");
            return 1;
        }
    }

    private static void MostrarContrasena(string titulo, string correo, string temporal)
    {
        Console.WriteLine();
        Console.WriteLine("==================================================================");
        Console.WriteLine($"  {titulo}");
        Console.WriteLine($"  Correo:               {correo}");
        Console.WriteLine($"  Contrasena temporal:  {temporal}");
        Console.WriteLine("  Anotela ahora: no se vuelve a mostrar. Al entrar, el sistema");
        Console.WriteLine("  pedira cambiarla por una propia.");
        Console.WriteLine("==================================================================");
        Console.WriteLine();
    }
}

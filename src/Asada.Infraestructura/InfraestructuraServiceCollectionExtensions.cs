using Asada.Aplicacion.Common;
using Asada.Infraestructura.Identidad;
using Asada.Infraestructura.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asada.Infraestructura;

/// <summary>
/// Punto unico de registro de todos los servicios de Asada.Infraestructura. Se llama una
/// sola vez desde Program.cs (builder.Services.AgregarInfraestructura(builder.Configuration)),
/// para que Program.cs no tenga que conocer los detalles de cada pieza (EF Core, Identity,
/// el proveedor de organizacion...).
/// </summary>
public static class InfraestructuraServiceCollectionExtensions
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("AsadaDb")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion 'AsadaDb' en la configuracion (appsettings.json).");

        servicios.AddDbContext<AsadaDbContext>(opciones => opciones
            .UseNpgsql(cadenaConexion)
            .UseSnakeCaseNamingConvention());

        servicios.AddHttpContextAccessor();
        servicios.AddScoped<IProveedorOrganizacion, ProveedorOrganizacion>();

        servicios
            .AddIdentity<Usuario, Rol>(opciones =>
            {
                // Reglas de contrasena razonables para un sistema administrativo interno;
                // se pueden endurecer en la seccion 5 (seguridad) del plan tecnico.
                opciones.Password.RequiredLength = 8;
                opciones.Password.RequireNonAlphanumeric = false;
                opciones.User.RequireUniqueEmail = true;
                opciones.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AsadaDbContext>()
            .AddClaimsPrincipalFactory<UsuarioClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        servicios.ConfigureApplicationCookie(opciones =>
        {
            opciones.LoginPath = "/cuenta/iniciar-sesion";
            opciones.AccessDeniedPath = "/cuenta/acceso-denegado";
            opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
            opciones.SlidingExpiration = true;
        });

        return servicios;
    }
}

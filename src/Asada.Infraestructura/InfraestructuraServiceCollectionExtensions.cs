using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Configuracion;
using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Organizaciones;
using Asada.Aplicacion.Periodos;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Tarifas;
using Asada.Aplicacion.Usuarios;
using Asada.Dominio.Reglas;
using Asada.Infraestructura.Documentos;
using Asada.Infraestructura.Identidad;
using Asada.Infraestructura.Persistence;
using Asada.Infraestructura.Persistence.Auditoria;
using Asada.Infraestructura.Persistence.Repositorios;
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

        // Sprint 5: el interceptor de auditoria se resuelve del mismo alcance que el DbContext
        // (una instancia por solicitud), porque lee el usuario actual y guarda estado entre el
        // "antes" y el "despues" de cada SaveChanges.
        servicios.AddScoped<InterceptorAuditoria>();
        servicios.AddDbContext<AsadaDbContext>((proveedor, opciones) => opciones
            .UseNpgsql(cadenaConexion)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(proveedor.GetRequiredService<InterceptorAuditoria>()));

        servicios.AddHttpContextAccessor();
        servicios.AddScoped<IProveedorOrganizacion, ProveedorOrganizacion>();
        servicios.AddScoped<IProveedorUsuario, ProveedorUsuario>();
        servicios.AddScoped<IRepositorioAbonados, RepositorioAbonados>();
        servicios.AddScoped<IRepositorioMediciones, RepositorioMediciones>();
        servicios.AddScoped<IRepositorioTarifas, RepositorioTarifas>();
        servicios.AddScoped<IRepositorioRecibos, RepositorioRecibos>();
        servicios.AddScoped<IRepositorioPeriodos, RepositorioPeriodos>();
        servicios.AddScoped<IRepositorioConfiguracion, RepositorioConfiguracion>();
        servicios.AddScoped<IRepositorioAuditoria, RepositorioAuditoria>();
        servicios.AddScoped<IRepositorioOrganizaciones, RepositorioOrganizaciones>();
        servicios.AddScoped<IGestorUsuarios, GestorUsuarios>();
        servicios.AddSingleton<IProveedorFecha, ProveedorFecha>();
        servicios.AddSingleton<IGeneradorPdfRecibos, GeneradorPdfRecibos>();

        servicios
            .AddIdentity<Usuario, Rol>(opciones =>
            {
                // Sprint 5b (decision del 4/11/2026): 10+ caracteres con al menos una letra y un numero,
                // sin vencimiento. La regla completa (letra, numero, sin el correo) la aplica
                // ValidadorContrasenaAsada; aqui solo el largo, y se apagan las reglas propias de
                // Identity (mayuscula, simbolo...) que no forman parte de la politica.
                opciones.Password.RequiredLength = ReglasContrasena.LargoMinimo;
                opciones.Password.RequireDigit = false;
                opciones.Password.RequireLowercase = false;
                opciones.Password.RequireUppercase = false;
                opciones.Password.RequireNonAlphanumeric = false;
                opciones.Password.RequiredUniqueChars = 1;

                // Bloqueo: 5 intentos fallidos seguidos -> 15 minutos (o hasta que el Administrador
                // lo desbloquee). Aplica tambien a los usuarios recien creados.
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opciones.Lockout.AllowedForNewUsers = true;

                opciones.User.RequireUniqueEmail = true;
                opciones.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AsadaDbContext>()
            .AddClaimsPrincipalFactory<UsuarioClaimsPrincipalFactory>()
            .AddSignInManager<SignInManagerAsada>()
            .AddPasswordValidator<ValidadorContrasenaAsada>()
            .AddErrorDescriber<DescriptorErroresIdentidad>()
            .AddDefaultTokenProviders();

        // Sprint 5b: cada minuto se revisa el sello de seguridad de las sesiones abiertas. Al
        // desactivar un usuario, cambiarle el rol o restablecer su contrasena se cambia el sello, y
        // su sesion se cierra (o recibe los permisos nuevos) en menos de un minuto. El valor por
        // defecto de Identity es 30 minutos.
        servicios.Configure<SecurityStampValidatorOptions>(opciones => opciones.ValidationInterval = TimeSpan.FromMinutes(1));

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

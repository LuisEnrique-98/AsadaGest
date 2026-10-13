using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Configuracion;
using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Periodos;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Tarifas;
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
        servicios.AddSingleton<IProveedorFecha, ProveedorFecha>();
        servicios.AddSingleton<IGeneradorPdfRecibos, GeneradorPdfRecibos>();

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

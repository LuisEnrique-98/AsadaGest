using Asada.Infraestructura;
using Asada.Infraestructura.Identidad;
using Asada.Infraestructura.Persistence;
using Asada.Infraestructura.Persistence.Seed;
using Asada.Presentacion;
using Asada.Presentacion.Components;
using Asada.Presentacion.Seguridad;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Sprint 6a: "migrar" y "restablecer-superadministrador" ejecutan un comando de mantenimiento y
// terminan, sin levantar el sitio (ver ComandosConsola). Se quitan de los argumentos para que no se
// confundan con valores de configuracion.
var comando = args.FirstOrDefault(ComandosConsola.EsComando);
var builder = WebApplication.CreateBuilder(args.Where(a => !ComandosConsola.EsComando(a)).ToArray());

// ---- Servicios ----
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
// Matriz de permisos (Sprint 5): una politica por accion sensible. Ver Seguridad/Permisos.cs.
builder.Services.AddAuthorization(opciones => opciones.AgregarPoliticasAsada());

// Registra AsadaDbContext, ASP.NET Core Identity, IProveedorOrganizacion y la configuracion
// de la cookie de autenticacion. Ver InfraestructuraServiceCollectionExtensions.
builder.Services.AgregarInfraestructura(builder.Configuration);

// Casos de uso de Asada.Aplicacion (Abonados, Propiedades, Servicios...).
builder.Services.AgregarCasosDeUso();
builder.Services.AddScoped<IRenovadorSesion, RenovadorSesion>();

// Sprint 6a: en el servidor, las claves que cifran la cookie de sesion y los tokens antiforgery se
// guardan en una carpeta persistente (un volumen de Docker). Sin esto, cada vez que se actualiza o
// reinicia el contenedor se generan claves nuevas: todos los usuarios quedan fuera de su sesion y
// los formularios abiertos fallan con "antiforgery token". En la PC de desarrollo no se configura y
// .NET usa su carpeta por defecto del perfil de usuario.
var carpetaClaves = builder.Configuration["Despliegue:CarpetaClaves"];
if (!string.IsNullOrWhiteSpace(carpetaClaves))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("AsadaGest")
        .PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves));
}

var app = builder.Build();

if (comando is not null)
{
    return await ComandosConsola.EjecutarAsync(comando, app);
}

// ---- Migraciones y datos iniciales (solo en Desarrollo) ----
// En Desarrollo, aplicar migraciones pendientes y sembrar datos automaticamente al arrancar
// ahorra el paso manual en cada sesion de trabajo. En el servidor (Sprint 6a) la aplicacion no
// migra sola: lo hace el comando "migrar" (ComandosConsola), que actualizar.sh ejecuta despues de
// un respaldo. Detras del proxy (Caddy), el esquema https y la IP real del visitante llegan en las
// cabeceras X-Forwarded-*; el contenedor las acepta con ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
// (ver Dockerfile), sin codigo adicional aqui.
if (app.Environment.IsDevelopment())
{
    using var alcanceInicio = app.Services.CreateScope();
    var db = alcanceInicio.ServiceProvider.GetRequiredService<AsadaDbContext>();
    await db.Database.MigrateAsync();
    await SeedInicial.EjecutarAsync(app.Services);
}

// ---- Canalizacion HTTP ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Sprint 5b: con una contrasena temporal solo se puede entrar a cambiarla. Ver Seguridad/CambioContrasenaObligatorio.cs.
app.UseCambioContrasenaObligatorio();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoint de cierre de sesion: lo postea el formulario de FormularioCerrarSesion.razor.
// Es un endpoint HTTP normal (no un componente Blazor) porque cerrar sesion implica escribir
// la cookie de autenticacion en la respuesta, algo que un componente interactivo (dentro de
// una conexion SignalR ya establecida) no puede hacer.
app.MapPost("/cuenta/cerrar-sesion", async (SignInManager<Usuario> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/cuenta/iniciar-sesion");
});

// Recibos en PDF (Sprint 4): /recibos/{id}/pdf y /recibos/pdf?anio=&mes=. Ver EndpointsRecibos.
app.MapearEndpointsRecibos();

// Sprint 6a: verificacion de salud para Docker (HEALTHCHECK del Dockerfile). Publica: solo dice si la
// aplicacion responde y llega a la base de datos, sin ningun dato.
app.MapGet("/salud", async (AsadaDbContext db, CancellationToken ct) =>
        await db.Database.CanConnectAsync(ct)
            ? Results.Text("ok")
            : Results.Text("sin conexion a la base de datos", statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.Run();
return 0;

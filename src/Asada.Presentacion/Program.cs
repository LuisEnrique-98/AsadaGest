using Asada.Infraestructura;
using Asada.Infraestructura.Identidad;
using Asada.Infraestructura.Persistence;
using Asada.Infraestructura.Persistence.Seed;
using Asada.Presentacion.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Servicios ----
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// Registra AsadaDbContext, ASP.NET Core Identity, IProveedorOrganizacion y la configuracion
// de la cookie de autenticacion. Ver InfraestructuraServiceCollectionExtensions.
builder.Services.AgregarInfraestructura(builder.Configuration);

var app = builder.Build();

// ---- Migraciones y datos iniciales (solo en Desarrollo) ----
// En Desarrollo, aplicar migraciones pendientes y sembrar datos automaticamente al arrancar
// ahorra el paso manual en cada sesion de trabajo. En un entorno real (Sprint 6, despliegue),
// esto se reemplaza por un paso explicito de "dotnet ef database update" en el pipeline.
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

app.Run();

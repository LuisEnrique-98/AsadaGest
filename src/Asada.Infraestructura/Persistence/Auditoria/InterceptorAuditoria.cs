using System.Globalization;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Asada.Infraestructura.Persistence.Auditoria;

/// <summary>
/// Bitacora de auditoria automatica (Sprint 5). Cada vez que AsadaDbContext guarda cambios, este
/// interceptor anota, por cada entidad del negocio creada, modificada o eliminada: quien, cuando,
/// que registro y que campos cambiaron (antes y despues). Asi ningun caso de uso, presente o
/// futuro, puede "olvidarse" de auditar.
///
/// Funciona en dos pasos porque el Id de un registro nuevo solo se conoce despues de guardarlo:
///   1. Antes de guardar (SavingChanges) se toma nota de los cambios.
///   2. Despues de guardar (SavedChanges) se escriben las filas de auditoria con un segundo
///      SaveChanges. Si el primero ocurre dentro de una transaccion explicita (p. ej. la
///      generacion de recibos), el segundo tambien, y se confirman juntos.
///
/// Se registra como Scoped (una instancia por solicitud, igual que el DbContext): guarda estado
/// entre los dos pasos.
///
/// No se auditan: las tablas de ASP.NET Core Identity (el inicio de sesion cambia contadores y
/// sellos de seguridad en cada acceso; la gestion de usuarios se audita aparte en el Sprint 5b),
/// los tramos de tarifa y las lineas de recibo (nunca cambian y se ven a traves de su tarifa o su
/// recibo) ni la propia bitacora.
/// </summary>
public sealed class InterceptorAuditoria(IProveedorUsuario usuario, IProveedorOrganizacion proveedorOrganizacion)
    : SaveChangesInterceptor
{
    private static readonly HashSet<Type> TiposAuditados =
    [
        typeof(Organizacion), typeof(Abonado), typeof(Propiedad), typeof(Servicio), typeof(Medidor),
        typeof(Periodo), typeof(Medicion), typeof(SeguimientoLectura), typeof(CategoriaServicio),
        typeof(Tarifa), typeof(Recibo),
    ];

    private sealed record Pendiente(EntityEntry Entrada, AccionAuditoria Accion, List<CambioAuditoria> Cambios, int? OrganizacionId);

    private List<Pendiente>? pendientes;
    private bool escribiendoBitacora;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        TomarNota(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        TomarNota(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (CrearRegistros(eventData.Context) is { } contexto) GuardarBitacora(() => contexto.SaveChanges());
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (CrearRegistros(eventData.Context) is { } contexto)
        {
            escribiendoBitacora = true;
            try { await contexto.SaveChangesAsync(cancellationToken); }
            finally { escribiendoBitacora = false; }
        }
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => pendientes = null;

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        pendientes = null;
        return Task.CompletedTask;
    }

    private void GuardarBitacora(Action guardar)
    {
        escribiendoBitacora = true;
        try { guardar(); }
        finally { escribiendoBitacora = false; }
    }

    private void TomarNota(DbContext? contexto)
    {
        if (contexto is null || escribiendoBitacora) return;

        pendientes = [];
        foreach (var entrada in contexto.ChangeTracker.Entries())
        {
            if (!TiposAuditados.Contains(entrada.Metadata.ClrType)) continue;

            var accion = entrada.State switch
            {
                EntityState.Added => AccionAuditoria.Creacion,
                EntityState.Modified => AccionAuditoria.Modificacion,
                EntityState.Deleted => AccionAuditoria.Eliminacion,
                _ => (AccionAuditoria?)null,
            };
            if (accion is null) continue;

            var cambios = new List<CambioAuditoria>();
            foreach (var propiedad in entrada.Properties)
            {
                var nombre = propiedad.Metadata.Name;
                if (nombre == "Id") continue;

                switch (accion)
                {
                    case AccionAuditoria.Creacion:
                        if (propiedad.CurrentValue is not null)
                            cambios.Add(new CambioAuditoria(nombre, null, Texto(propiedad.CurrentValue)));
                        break;
                    case AccionAuditoria.Modificacion:
                        var antes = Texto(propiedad.OriginalValue);
                        var despues = Texto(propiedad.CurrentValue);
                        if (propiedad.IsModified && antes != despues)
                            cambios.Add(new CambioAuditoria(nombre, antes, despues));
                        break;
                    case AccionAuditoria.Eliminacion:
                        if (propiedad.OriginalValue is not null)
                            cambios.Add(new CambioAuditoria(nombre, Texto(propiedad.OriginalValue), null));
                        break;
                }
            }

            // Un "Modified" sin diferencias reales (se asigno el mismo valor) no se anota.
            if (accion == AccionAuditoria.Modificacion && cambios.Count == 0) continue;

            pendientes.Add(new Pendiente(entrada, accion.Value, cambios, OrganizacionDe(entrada)));
        }
    }

    /// <summary>Agrega las filas de auditoria al contexto; devuelve el contexto si hay algo que guardar.</summary>
    private DbContext? CrearRegistros(DbContext? contexto)
    {
        var lista = pendientes;
        pendientes = null;
        if (contexto is null || escribiendoBitacora || lista is null || lista.Count == 0) return null;

        var ahora = DateTime.UtcNow;
        var usuarioId = usuario.UsuarioId;
        var nombre = usuario.Nombre;
        foreach (var p in lista)
        {
            contexto.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                OrganizacionId = p.OrganizacionId ?? OrganizacionDe(p.Entrada),
                UsuarioId = usuarioId,
                UsuarioNombre = nombre,
                Fecha = ahora,
                Accion = p.Accion,
                Entidad = p.Entrada.Metadata.ClrType.Name,
                EntidadId = Convert.ToInt64(p.Entrada.Property("Id").CurrentValue, CultureInfo.InvariantCulture),
                Cambios = CambioAuditoria.Serializar(p.Cambios),
            });
        }
        return contexto;
    }

    private int? OrganizacionDe(EntityEntry entrada)
    {
        if (entrada.Entity is Organizacion o) return o.Id == 0 ? null : o.Id;
        var propiedad = entrada.Metadata.FindProperty("OrganizacionId");
        return propiedad is not null && entrada.Property(propiedad.Name).CurrentValue is int id
            ? id
            : proveedorOrganizacion.OrganizacionId;
    }

    private static string? Texto(object? valor) => valor switch
    {
        null => null,
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + (d.Kind == DateTimeKind.Utc ? " UTC" : ""),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        decimal m => m.ToString("0.####", CultureInfo.InvariantCulture),
        bool b => b ? "Si" : "No",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString(),
    };
}

using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Infraestructura.Identidad;
using Asada.Infraestructura.Persistence.Configuraciones;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Asada.Infraestructura.Persistence;

/// <summary>
/// Contexto de base de datos de toda la plataforma. Hereda de IdentityDbContext en vez de
/// DbContext simple para obtener, ya resueltas, las tablas de autenticacion de ASP.NET Core
/// Identity (usuarios, roles, claims, logins, tokens).
///
/// La pieza mas importante de esta clase es el HasQueryFilter aplicado en OnModelCreating:
/// se aplica una sola vez, a nivel de modelo, y de ahi en adelante EF Core lo agrega
/// automaticamente a cualquier consulta sobre Organizacion o Usuario -- ningun caso de uso
/// futuro necesita acordarse de filtrar por organizacion_id manualmente (RB-015 del SRS).
/// Cada entidad nueva que pertenezca a una organizacion (Abonado, Propiedad y Servicio
/// desde el Sprint 1; Tarifa, Recibo... despues) debe sumarse a este mismo patron.
/// </summary>
public class AsadaDbContext(DbContextOptions<AsadaDbContext> options, IProveedorOrganizacion proveedorOrganizacion)
    : IdentityDbContext<Usuario, Rol, int>(options)
{
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<Abonado> Abonados => Set<Abonado>();
    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Medidor> Medidores => Set<Medidor>();
    public DbSet<Periodo> Periodos => Set<Periodo>();
    public DbSet<Medicion> Mediciones => Set<Medicion>();
    public DbSet<SeguimientoLectura> SeguimientosLectura => Set<SeguimientoLectura>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new OrganizacionConfiguracion());
        builder.ApplyConfiguration(new UsuarioConfiguracion());
        builder.ApplyConfiguration(new AbonadoConfiguracion());
        builder.ApplyConfiguration(new PropiedadConfiguracion());
        builder.ApplyConfiguration(new ServicioConfiguracion());
        builder.ApplyConfiguration(new MedidorConfiguracion());
        builder.ApplyConfiguration(new PeriodoConfiguracion());
        builder.ApplyConfiguration(new MedicionConfiguracion());
        builder.ApplyConfiguration(new SeguimientoLecturaConfiguracion());

        // Renombrar las tablas propias de Identity a nombres en espanol, consistentes con
        // el resto del esquema. Las columnas internas (email, password_hash, etc.) se dejan
        // con los nombres que trae ASP.NET Core Identity -- traducirlas obligaria a mantener
        // un mapeo manual de cada version futura de Identity, con poco beneficio real.
        builder.Entity<Rol>().ToTable("roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("usuario_roles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("usuario_claims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("usuario_logins");
        builder.Entity<IdentityUserToken<int>>().ToTable("usuario_tokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("rol_claims");

        // Filtro global de aislamiento multiorganizacion.
        builder.Entity<Organizacion>().HasQueryFilter(o =>
            proveedorOrganizacion.EsSuperadministrador || o.Id == proveedorOrganizacion.OrganizacionId);

        // Sprint 1: mismo filtro para cada entidad que pertenece a una organizacion. Se repite
        // organizacion_id en Propiedad y Servicio a proposito, para que el filtro sea una sola
        // comparacion de columna (sin joins) y ninguna consulta pueda saltarselo por accidente.
        builder.Entity<Abonado>().HasQueryFilter(a =>
            proveedorOrganizacion.EsSuperadministrador || a.OrganizacionId == proveedorOrganizacion.OrganizacionId);
        builder.Entity<Propiedad>().HasQueryFilter(p =>
            proveedorOrganizacion.EsSuperadministrador || p.OrganizacionId == proveedorOrganizacion.OrganizacionId);
        builder.Entity<Servicio>().HasQueryFilter(s =>
            proveedorOrganizacion.EsSuperadministrador || s.OrganizacionId == proveedorOrganizacion.OrganizacionId);

        // Sprint 2: medidores, periodos, lecturas y seguimientos, con el mismo patron.
        builder.Entity<Medidor>().HasQueryFilter(m =>
            proveedorOrganizacion.EsSuperadministrador || m.OrganizacionId == proveedorOrganizacion.OrganizacionId);
        builder.Entity<Periodo>().HasQueryFilter(p =>
            proveedorOrganizacion.EsSuperadministrador || p.OrganizacionId == proveedorOrganizacion.OrganizacionId);
        builder.Entity<Medicion>().HasQueryFilter(m =>
            proveedorOrganizacion.EsSuperadministrador || m.OrganizacionId == proveedorOrganizacion.OrganizacionId);
        builder.Entity<SeguimientoLectura>().HasQueryFilter(s =>
            proveedorOrganizacion.EsSuperadministrador || s.OrganizacionId == proveedorOrganizacion.OrganizacionId);

        // A proposito, Usuario NO lleva HasQueryFilter todavia, aunque tiene OrganizacionId:
        // UserManager/SignInManager consultan esta misma tabla para autenticar, y en ese
        // momento de la solicitud (antes de iniciar sesion) todavia no hay ningun usuario
        // autenticado del cual derivar una organizacion -- si el filtro estuviera activo,
        // bloquearia el propio login (nadie podria autenticarse jamas, salvo el
        // Superadministrador). El aislamiento entre organizaciones para listados
        // administrativos de usuarios se resolvera con un caso de uso propio (Sprint 5,
        // matriz de permisos), que si puede filtrar explicitamente por organizacion_id.
    }
}

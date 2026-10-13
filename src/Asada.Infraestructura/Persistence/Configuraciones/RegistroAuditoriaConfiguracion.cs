using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class RegistroAuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("auditoria");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UsuarioNombre).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Accion).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Entidad).HasMaxLength(50).IsRequired();
        // jsonb: PostgreSQL valida que sea JSON y permite consultarlo si algun dia hace falta.
        builder.Property(a => a.Cambios).HasColumnType("jsonb").IsRequired();

        // Busquedas tipicas: lo mas reciente de la ASADA, y el historial de un registro.
        builder.HasIndex(a => new { a.OrganizacionId, a.Fecha });
        builder.HasIndex(a => new { a.Entidad, a.EntidadId });

        // Sin llaves foraneas a usuarios ni a la entidad auditada a proposito: la bitacora debe
        // sobrevivir a cualquier cambio en esas tablas. Solo la organizacion es una FK.
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(a => a.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
    }
}

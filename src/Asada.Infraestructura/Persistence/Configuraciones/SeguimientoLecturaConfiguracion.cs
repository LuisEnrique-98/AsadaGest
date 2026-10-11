using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class SeguimientoLecturaConfiguracion : IEntityTypeConfiguration<SeguimientoLectura>
{
    public void Configure(EntityTypeBuilder<SeguimientoLectura> builder)
    {
        builder.ToTable("seguimientos_lectura");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Decision).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.Nota).HasMaxLength(400);

        builder.HasIndex(s => new { s.ServicioId, s.PeriodoId }).IsUnique();
        builder.HasIndex(s => s.OrganizacionId);

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(s => s.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(s => s.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Periodo>().WithMany().HasForeignKey(s => s.PeriodoId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class ServicioConfiguracion : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.ToTable("servicios");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Observaciones).HasMaxLength(400);
        builder.Property(s => s.MontoPendiente).HasPrecision(12, 2);
        builder.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(s => s.OrganizacionId);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(s => s.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Propiedad)
            .WithMany(p => p.Servicios)
            .HasForeignKey(s => s.PropiedadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

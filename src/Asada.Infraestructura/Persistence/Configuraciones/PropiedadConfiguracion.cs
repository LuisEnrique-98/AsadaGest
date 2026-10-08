using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class PropiedadConfiguracion : IEntityTypeConfiguration<Propiedad>
{
    public void Configure(EntityTypeBuilder<Propiedad> builder)
    {
        builder.ToTable("propiedades");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Direccion).HasMaxLength(400).IsRequired();
        builder.Property(p => p.Provincia).HasMaxLength(80);
        builder.Property(p => p.Canton).HasMaxLength(80);
        builder.Property(p => p.Distrito).HasMaxLength(80);
        builder.Property(p => p.Referencia).HasMaxLength(400);
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => p.OrganizacionId);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(p => p.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: un abonado con propiedades no se puede borrar (no hay borrado fisico).
        builder.HasOne(p => p.Abonado)
            .WithMany(a => a.Propiedades)
            .HasForeignKey(p => p.AbonadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

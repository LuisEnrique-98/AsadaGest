using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class CategoriaServicioConfiguracion : IEntityTypeConfiguration<CategoriaServicio>
{
    public void Configure(EntityTypeBuilder<CategoriaServicio> builder)
    {
        builder.ToTable("categorias_servicio");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Descripcion).HasMaxLength(200);
        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Nombre unico por ASADA (el caso de uso ademas compara sin distinguir mayusculas).
        builder.HasIndex(c => new { c.OrganizacionId, c.Nombre }).IsUnique();

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(c => c.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
    }
}

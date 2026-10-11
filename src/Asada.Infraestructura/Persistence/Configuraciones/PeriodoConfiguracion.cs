using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class PeriodoConfiguracion : IEntityTypeConfiguration<Periodo>
{
    public void Configure(EntityTypeBuilder<Periodo> builder)
    {
        builder.ToTable("periodos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Las propiedades calculadas (ventana de lectura) no son columnas.
        builder.Ignore(p => p.InicioVentanaLectura);
        builder.Ignore(p => p.FinVentanaLectura);

        builder.HasIndex(p => new { p.OrganizacionId, p.Anio, p.Mes }).IsUnique();
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(p => p.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
    }
}

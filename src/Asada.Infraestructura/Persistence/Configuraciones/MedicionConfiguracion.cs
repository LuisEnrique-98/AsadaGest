using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class MedicionConfiguracion : IEntityTypeConfiguration<Medicion>
{
    public void Configure(EntityTypeBuilder<Medicion> builder)
    {
        builder.ToTable("mediciones");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Lectura).HasPrecision(12, 3);
        builder.Property(m => m.Consumo).HasPrecision(12, 3);
        builder.Property(m => m.MotivosRevision).HasMaxLength(800);
        builder.Property(m => m.Observaciones).HasMaxLength(400);

        // Una sola lectura por servicio y periodo.
        builder.HasIndex(m => new { m.ServicioId, m.PeriodoId }).IsUnique();
        builder.HasIndex(m => m.OrganizacionId);
        builder.HasIndex(m => m.PeriodoId);

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(m => m.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(m => m.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Medidor>().WithMany().HasForeignKey(m => m.MedidorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Periodo>().WithMany().HasForeignKey(m => m.PeriodoId).OnDelete(DeleteBehavior.Restrict);
    }
}

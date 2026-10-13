using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
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

        // Sprint 5: estado, quien la registro, ultima correccion y anulacion. El valor por defecto
        // de la columna es 'Vigente' para que las lecturas que ya existian al migrar sigan contando.
        builder.Property(m => m.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EstadoMedicion.Vigente)
            .IsRequired();
        builder.Property(m => m.RegistradaPorNombre).HasMaxLength(150);
        builder.Property(m => m.CorregidaPorNombre).HasMaxLength(150);
        builder.Property(m => m.MotivoCorreccion).HasMaxLength(400);
        builder.Property(m => m.AnuladaPorNombre).HasMaxLength(150);
        builder.Property(m => m.MotivoAnulacion).HasMaxLength(400);

        // Una sola lectura VIGENTE por servicio y periodo: una anulada no impide registrar otra.
        builder.HasIndex(m => new { m.ServicioId, m.PeriodoId })
            .IsUnique()
            .HasFilter("estado = 'Vigente'");
        builder.HasIndex(m => m.OrganizacionId);
        builder.HasIndex(m => m.PeriodoId);

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(m => m.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(m => m.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Medidor>().WithMany().HasForeignKey(m => m.MedidorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Periodo>().WithMany().HasForeignKey(m => m.PeriodoId).OnDelete(DeleteBehavior.Restrict);
    }
}

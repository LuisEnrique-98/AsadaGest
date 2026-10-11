using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class MedidorConfiguracion : IEntityTypeConfiguration<Medidor>
{
    public void Configure(EntityTypeBuilder<Medidor> builder)
    {
        builder.ToTable("medidores");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.NumeroSerie).HasMaxLength(50).IsRequired();
        builder.Property(m => m.LecturaInicial).HasPrecision(12, 3);
        builder.Property(m => m.LecturaFinal).HasPrecision(12, 3);
        builder.Property(m => m.Observaciones).HasMaxLength(400);
        builder.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Estado y FechaRetiro/LecturaFinal tienen setter privado (solo Retirar() los cambia);
        // EF Core los rellena igual por acceso al campo.

        // Un numero de serie fisico es unico dentro de una ASADA.
        builder.HasIndex(m => new { m.OrganizacionId, m.NumeroSerie }).IsUnique();
        builder.HasIndex(m => m.ServicioId, "ix_medidores_servicio_id");

        // A lo sumo un medidor activo por servicio: garantizado tambien en la base de datos,
        // no solo en el caso de uso (indice unico filtrado de PostgreSQL).
        builder.HasIndex(m => m.ServicioId, "ix_medidores_un_activo_por_servicio")
            .IsUnique()
            .HasFilter("estado = 'Activo'");

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(m => m.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(m => m.ServicioId).OnDelete(DeleteBehavior.Restrict);
    }
}

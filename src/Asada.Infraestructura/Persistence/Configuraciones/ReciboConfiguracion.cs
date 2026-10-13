using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class ReciboConfiguracion : IEntityTypeConfiguration<Recibo>
{
    public void Configure(EntityTypeBuilder<Recibo> builder)
    {
        builder.ToTable("recibos");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Numero).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Datos congelados: mismos largos que las columnas de origen.
        builder.Property(r => r.AbonadoCodigo).HasMaxLength(20).IsRequired();
        builder.Property(r => r.AbonadoNombre).HasMaxLength(150).IsRequired();
        builder.Property(r => r.PropiedadDireccion).HasMaxLength(400).IsRequired();
        builder.Property(r => r.CategoriaNombre).HasMaxLength(60).IsRequired();
        builder.Property(r => r.NumeroSerieMedidor).HasMaxLength(50).IsRequired();
        builder.Property(r => r.MotivosRevision).HasMaxLength(800);

        builder.Property(r => r.LecturaAnterior).HasPrecision(12, 3);
        builder.Property(r => r.LecturaActual).HasPrecision(12, 3);
        builder.Property(r => r.Consumo).HasPrecision(12, 3);
        builder.Property(r => r.SubtotalAgua).HasPrecision(12, 2);
        builder.Property(r => r.MontoHidrante).HasPrecision(12, 2);
        builder.Property(r => r.Total).HasPrecision(12, 2);

        // Un solo recibo emitido por servicio y periodo. El indice es filtrado para que, cuando
        // exista la anulacion (Sprint 5), un recibo anulado no impida emitir el que lo reemplaza.
        // Es tambien lo que detecta a dos usuarios generando el mismo periodo a la vez.
        builder.HasIndex(r => new { r.ServicioId, r.PeriodoId })
            .IsUnique()
            .HasFilter("estado = 'Emitido'");
        builder.HasIndex(r => new { r.OrganizacionId, r.Numero }).IsUnique();
        builder.HasIndex(r => r.PeriodoId);
        builder.HasIndex(r => r.TarifaId);

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(r => r.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(r => r.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Periodo>().WithMany().HasForeignKey(r => r.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Medicion>().WithMany().HasForeignKey(r => r.MedicionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tarifa>().WithMany().HasForeignKey(r => r.TarifaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Detalles).WithOne().HasForeignKey(d => d.ReciboId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReciboDetalleConfiguracion : IEntityTypeConfiguration<ReciboDetalle>
{
    public void Configure(EntityTypeBuilder<ReciboDetalle> builder)
    {
        builder.ToTable("recibo_detalles");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Concepto).HasMaxLength(120).IsRequired();
        builder.Property(d => d.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Cantidad).HasPrecision(12, 3);
        builder.Property(d => d.PrecioUnitario).HasPrecision(12, 4);
        builder.Property(d => d.Monto).HasPrecision(12, 2);

        builder.HasIndex(d => new { d.ReciboId, d.Orden }).IsUnique();
    }
}

using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class TarifaConfiguracion : IEntityTypeConfiguration<Tarifa>
{
    public void Configure(EntityTypeBuilder<Tarifa> builder)
    {
        builder.ToTable("tarifas");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CargoFijo).HasPrecision(12, 2);
        // Precios unitarios con 4 decimales: un precio por m³ (de agua o de hidrantes) puede
        // traer centimos fraccionarios en el pliego; los montos del recibo si van a 2 decimales.
        builder.Property(t => t.ValorHidrante).HasPrecision(12, 4);
        builder.Property(t => t.ModoHidrante).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Observaciones).HasMaxLength(400);

        // Una sola version por categoria y fecha de vigencia.
        builder.HasIndex(t => new { t.CategoriaId, t.VigenteDesde }).IsUnique();
        builder.HasIndex(t => t.OrganizacionId);

        builder.HasOne<Organizacion>().WithMany().HasForeignKey(t => t.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Categoria).WithMany().HasForeignKey(t => t.CategoriaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Bloques).WithOne().HasForeignKey(b => b.TarifaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TarifaBloqueConfiguracion : IEntityTypeConfiguration<TarifaBloque>
{
    public void Configure(EntityTypeBuilder<TarifaBloque> builder)
    {
        builder.ToTable("tarifa_bloques");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.LimiteSuperior).HasPrecision(12, 3);
        builder.Property(b => b.PrecioPorMetroCubico).HasPrecision(12, 4);

        builder.HasIndex(b => new { b.TarifaId, b.Orden }).IsUnique();
    }
}

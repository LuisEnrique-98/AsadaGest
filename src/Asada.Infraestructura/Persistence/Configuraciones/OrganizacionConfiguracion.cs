using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class OrganizacionConfiguracion : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> builder)
    {
        builder.ToTable("organizaciones");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Codigo).HasMaxLength(10).IsRequired();
        builder.HasIndex(o => o.Codigo).IsUnique();

        builder.Property(o => o.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(o => o.CedulaJuridica).HasMaxLength(20);
        builder.Property(o => o.Provincia).HasMaxLength(80);
        builder.Property(o => o.Canton).HasMaxLength(80);
        builder.Property(o => o.Distrito).HasMaxLength(80);
        builder.Property(o => o.Direccion).HasMaxLength(400);
        builder.Property(o => o.Telefono).HasMaxLength(30);
        builder.Property(o => o.Correo).HasMaxLength(150);

        builder.Property(o => o.UltimoCorrelativoAbonado).HasDefaultValue(0).IsRequired();

        // Se guarda como texto (p. ej. "Activa") en vez de un entero plano, para que el
        // valor sea legible directamente en la base de datos sin tener que consultar el enum.
        builder.Property(o => o.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}

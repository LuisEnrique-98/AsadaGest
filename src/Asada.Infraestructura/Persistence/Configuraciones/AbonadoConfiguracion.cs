using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class AbonadoConfiguracion : IEntityTypeConfiguration<Abonado>
{
    public void Configure(EntityTypeBuilder<Abonado> builder)
    {
        builder.ToTable("abonados");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Identificacion).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Telefono).HasMaxLength(30);
        builder.Property(a => a.Correo).HasMaxLength(150);
        builder.Property(a => a.Direccion).HasMaxLength(400);

        builder.Property(a => a.TipoIdentificacion).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Los correlativos y las identificaciones son unicos DENTRO de cada ASADA, no globalmente:
        // dos organizaciones distintas pueden tener su propio abonado numero 1 y atender a la
        // misma persona (RB-015, aislamiento multiorganizacion).
        builder.HasIndex(a => new { a.OrganizacionId, a.Correlativo }).IsUnique();
        builder.HasIndex(a => new { a.OrganizacionId, a.Codigo }).IsUnique();
        builder.HasIndex(a => new { a.OrganizacionId, a.TipoIdentificacion, a.Identificacion }).IsUnique();

        builder.HasOne(a => a.Organizacion)
            .WithMany()
            .HasForeignKey(a => a.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

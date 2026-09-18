using Asada.Infraestructura.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asada.Infraestructura.Persistence.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.Property(u => u.Nombre).HasMaxLength(150).IsRequired();

        builder.Property(u => u.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // OrganizacionId es null unicamente para el Superadministrador; por eso la relacion
        // es opcional (no HasRequired) y el borrado es Restrict: nunca se debe poder eliminar
        // una organizacion que todavia tiene usuarios asignados.
        builder.HasOne(u => u.Organizacion)
            .WithMany()
            .HasForeignKey(u => u.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

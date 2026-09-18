using Asada.Aplicacion.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Asada.Infraestructura.Persistence;

/// <summary>
/// Le indica a la herramienta "dotnet ef" como construir un AsadaDbContext fuera de la
/// aplicacion en ejecucion (por ejemplo, al generar una migracion). Sin esta fabrica, "dotnet
/// ef" intenta invocar el Program.cs real de Asada.Presentacion, lo cual a veces falla en
/// proyectos Blazor Server por como arrancan los servicios; con la fabrica, el comando
/// funciona siempre, de forma predecible, sin necesitar una base de datos corriendo
/// (generar una migracion no ejecuta ninguna consulta, solo lee el modelo de C#).
/// </summary>
public class AsadaDbContextFactoryDiseno : IDesignTimeDbContextFactory<AsadaDbContext>
{
    public AsadaDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<AsadaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=asada_gestion;Username=asada;Password=asada_dev_local")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AsadaDbContext(opciones, new ProveedorOrganizacionDiseno());
    }

    /// <summary>Proveedor "sin organizacion" usado solo para poder construir el modelo en diseno.</summary>
    private class ProveedorOrganizacionDiseno : IProveedorOrganizacion
    {
        public int? OrganizacionId => null;
        public bool EsSuperadministrador => true;
    }
}

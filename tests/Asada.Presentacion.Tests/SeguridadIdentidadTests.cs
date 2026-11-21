using System.Security.Claims;
using Asada.Infraestructura.Identidad;
using Asada.Infraestructura.Persistence.Seed;
using Microsoft.AspNetCore.Http;

namespace Asada.Presentacion.Tests;

public class SuperadministradorTests
{
    private static ProveedorOrganizacion Proveedor(bool autenticado, params Claim[] claims)
    {
        var identidad = autenticado ? new ClaimsIdentity(claims, "Prueba") : new ClaimsIdentity(claims);
        var contexto = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) };
        return new ProveedorOrganizacion(new HttpContextAccessor { HttpContext = contexto });
    }

    [Fact]
    public void ConElRolYSinAsada_EsSuperadministrador()
        => Assert.True(Proveedor(true, new Claim(ClaimTypes.Role, NombresRol.Superadministrador)).EsSuperadministrador);

    [Fact]
    public void SinAsadaYSinElRol_NoEsSuperadministrador()
    {
        // M-1: una sesion sin el claim de organizacion por un error no debe ver todas las ASADAs.
        Assert.False(Proveedor(true).EsSuperadministrador);
        Assert.False(Proveedor(true, new Claim(ClaimTypes.Role, NombresRol.Administrador)).EsSuperadministrador);
    }

    [Fact]
    public void ConAsada_NoEsSuperadministrador()
    {
        var proveedor = Proveedor(true,
            new Claim(ClaimTypes.Role, NombresRol.Superadministrador),
            new Claim(ClaimsAsada.OrganizacionId, "3"));

        Assert.False(proveedor.EsSuperadministrador);
        Assert.Equal(3, proveedor.OrganizacionId);
    }

    [Fact]
    public void SinSesion_NoEsSuperadministrador()
        => Assert.False(Proveedor(false, new Claim(ClaimTypes.Role, NombresRol.Superadministrador)).EsSuperadministrador);
}

public class SeedInicialTests
{
    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=asada_gestion;Username=asada;Password=x", true)]
    [InlineData("Host=LOCALHOST;Database=asada_gestion", true)]
    [InlineData("Host=127.0.0.1;Database=asada_gestion", true)]
    [InlineData("Host=::1;Database=asada_gestion", true)]
    [InlineData("Host=localhost:5432;Database=asada_gestion", true)]
    [InlineData("Host=localhost,127.0.0.1;Database=asada_gestion", true)]
    [InlineData("Host=postgres;Port=5432;Database=asada_gestion", false)]
    [InlineData("Host=10.0.0.5;Database=asada_gestion", false)]
    [InlineData("Host=localhost,postgres;Database=asada_gestion", false)]
    [InlineData("Host=localhost.ejemplo.com;Database=asada_gestion", false)]
    [InlineData("Database=asada_gestion", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("esto no es una cadena de conexion", false)]
    public void EsBaseLocal(string? cadena, bool esperado) => Assert.Equal(esperado, SeedInicial.EsBaseLocal(cadena));
}

using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;

namespace Asada.Dominio.Tests;

public class OrganizacionTests
{
    [Fact]
    public void UnaOrganizacionNueva_EmpiezaActiva()
    {
        var organizacion = new Organizacion
        {
            Codigo = "CB",
            Nombre = "ASADA Cuatro Bocas",
        };

        Assert.Equal(EstadoOrganizacion.Activa, organizacion.Estado);
        Assert.True(organizacion.EstaActiva);
    }

    [Fact]
    public void UnaOrganizacionInactiva_NoEstaActiva()
    {
        var organizacion = new Organizacion
        {
            Codigo = "CB",
            Nombre = "ASADA Cuatro Bocas",
            Estado = EstadoOrganizacion.Inactiva,
        };

        Assert.False(organizacion.EstaActiva);
    }
}

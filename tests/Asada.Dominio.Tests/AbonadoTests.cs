using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;

namespace Asada.Dominio.Tests;

public class AbonadoTests
{
    [Fact]
    public void GenerarCodigo_UsaPrefijoDeLaOrganizacionYSeisDigitos()
        => Assert.Equal("CB-AB-000125", Abonado.GenerarCodigo("CB", 125));

    [Fact]
    public void UnAbonadoNuevo_EmpiezaActivo()
    {
        var abonado = new Abonado { Identificacion = "102340567", Nombre = "Maria Rojas" };
        Assert.Equal(EstadoRegistro.Activo, abonado.Estado);
    }

    [Fact]
    public void PropiedadesYServicios_HeredanLaOrganizacionDelAbonado()
    {
        var abonado = new Abonado { OrganizacionId = 7, Identificacion = "102340567", Nombre = "Maria Rojas" };

        var propiedad = abonado.AgregarPropiedad("Frente a la escuela");
        var servicio = propiedad.AgregarServicio(new DateOnly(2026, 1, 15));

        Assert.Equal(7, propiedad.OrganizacionId);
        Assert.Equal(7, servicio.OrganizacionId);
        Assert.Single(abonado.Propiedades);
        Assert.Single(propiedad.Servicios);
    }

    [Fact]
    public void LaMorosidad_ViveEnElServicio_NoEnElAbonado()
    {
        var abonado = new Abonado { OrganizacionId = 1, Identificacion = "102340567", Nombre = "Maria Rojas" };
        var propiedad = abonado.AgregarPropiedad("Casa 1");

        var alDia = propiedad.AgregarServicio(new DateOnly(2026, 1, 1));
        var moroso = propiedad.AgregarServicio(new DateOnly(2026, 1, 1), esMoroso: true,
            mensualidadesPendientes: 3, montoPendiente: 15000m);

        Assert.False(alDia.EsMoroso);
        Assert.True(moroso.EsMoroso);
        Assert.Equal(3, moroso.MensualidadesPendientes);
    }
}

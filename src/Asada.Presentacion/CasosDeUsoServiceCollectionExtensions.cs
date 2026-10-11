using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Mediciones;

namespace Asada.Presentacion;

/// <summary>
/// Registro de los casos de uso de Asada.Aplicacion. Asada.Aplicacion no referencia ningun
/// paquete de inyeccion de dependencias a proposito (se mantiene sin dependencias externas),
/// asi que el registro vive aqui, donde ya existe el contenedor de servicios.
/// </summary>
public static class CasosDeUsoServiceCollectionExtensions
{
    public static IServiceCollection AgregarCasosDeUso(this IServiceCollection servicios)
    {
        servicios.AddScoped<CrearAbonadoCasoUso>();
        servicios.AddScoped<BuscarAbonadosCasoUso>();
        servicios.AddScoped<ObtenerExpedienteAbonadoCasoUso>();
        servicios.AddScoped<AgregarPropiedadCasoUso>();
        servicios.AddScoped<AgregarServicioCasoUso>();
        servicios.AddScoped<ActualizarAbonadoCasoUso>();
        servicios.AddScoped<CambiarEstadoAbonadoCasoUso>();

        // Sprint 2: medidores y lecturas.
        servicios.AddScoped<InstalarMedidorCasoUso>();
        servicios.AddScoped<RetirarMedidorCasoUso>();
        servicios.AddScoped<ObtenerMedidoresServicioCasoUso>();
        servicios.AddScoped<RegistrarLecturaCasoUso>();
        servicios.AddScoped<ObtenerLecturasPeriodoCasoUso>();
        servicios.AddScoped<RegistrarSeguimientoLecturaCasoUso>();
        return servicios;
    }
}

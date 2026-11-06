using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Configuracion;
using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Organizaciones;
using Asada.Aplicacion.Periodos;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Tarifas;
using Asada.Aplicacion.Usuarios;

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

        // Sprint 3: categorias, tarifas y recibos.
        servicios.AddScoped<CrearCategoriaCasoUso>();
        servicios.AddScoped<ObtenerPanelTarifasCasoUso>();
        servicios.AddScoped<CrearTarifaCasoUso>();
        servicios.AddScoped<ObtenerTarifaCasoUso>();
        servicios.AddScoped<SimularReciboCasoUso>();
        servicios.AddScoped<ObtenerCategoriaServicioCasoUso>();
        servicios.AddScoped<AsignarCategoriaServicioCasoUso>();
        servicios.AddScoped<ObtenerFacturacionPeriodoCasoUso>();
        servicios.AddScoped<GenerarRecibosPeriodoCasoUso>();
        servicios.AddScoped<ObtenerReciboCasoUso>();

        // Sprint 4: recibos en PDF.
        servicios.AddScoped<DescargarPdfReciboCasoUso>();
        servicios.AddScoped<DescargarPdfRecibosPeriodoCasoUso>();

        // Sprint 5a: anulaciones, correcciones, cierre de periodo, configuracion y auditoria.
        servicios.AddScoped<ObtenerLecturaCasoUso>();
        servicios.AddScoped<CorregirLecturaCasoUso>();
        servicios.AddScoped<AnularLecturaCasoUso>();
        servicios.AddScoped<AnularReciboCasoUso>();
        servicios.AddScoped<ObtenerCierrePeriodoCasoUso>();
        servicios.AddScoped<CerrarPeriodoCasoUso>();
        servicios.AddScoped<ReabrirPeriodoCasoUso>();
        servicios.AddScoped<ObtenerConfiguracionCasoUso>();
        servicios.AddScoped<ActualizarConfiguracionCasoUso>();
        servicios.AddScoped<BuscarAuditoriaCasoUso>();

        // Sprint 5b: usuarios de cada ASADA y ASADAs de la plataforma.
        servicios.AddSingleton<IGeneradorContrasena, GeneradorContrasena>();
        servicios.AddScoped<ListarUsuariosCasoUso>();
        servicios.AddScoped<ObtenerUsuarioCasoUso>();
        servicios.AddScoped<CrearUsuarioCasoUso>();
        servicios.AddScoped<ActualizarUsuarioCasoUso>();
        servicios.AddScoped<CambiarEstadoUsuarioCasoUso>();
        servicios.AddScoped<RestablecerContrasenaCasoUso>();
        servicios.AddScoped<DesbloquearUsuarioCasoUso>();
        servicios.AddScoped<CambiarContrasenaPropiaCasoUso>();
        servicios.AddScoped<ListarOrganizacionesCasoUso>();
        servicios.AddScoped<RegistrarAsadaCasoUso>();
        return servicios;
    }
}

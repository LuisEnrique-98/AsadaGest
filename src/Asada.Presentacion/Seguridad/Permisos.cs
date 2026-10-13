using Asada.Infraestructura.Identidad;
using Microsoft.AspNetCore.Authorization;

namespace Asada.Presentacion.Seguridad;

/// <summary>
/// Matriz de permisos de la plataforma (correccion C-06 del SRS, &#167;23): cada accion sensible
/// tiene un permiso con nombre, y aqui, en un solo lugar, se dice que roles lo tienen. Las paginas
/// no escriben nombres de rol: piden un permiso ([Authorize(Policy = Permisos.AnularRecibos)],
/// &lt;AuthorizeView Policy="@Permisos.AnularRecibos"&gt;). Cambiar quien puede hacer algo es
/// cambiar una linea de esta clase. La misma lista se muestra en la pagina /permisos.
///
/// Decisiones del 13 de octubre de 2026: anular recibos, corregir o anular lecturas y cerrar o
/// reabrir periodos es solo del Administrador.
/// </summary>
public static class Permisos
{
    public const string Consultar = "Consultar";
    public const string EditarAbonados = "EditarAbonados";
    public const string CambiarEstadoAbonados = "CambiarEstadoAbonados";
    public const string GestionarMedidores = "GestionarMedidores";
    public const string RegistrarLecturas = "RegistrarLecturas";
    public const string CorregirLecturas = "CorregirLecturas";
    public const string AsignarCategoria = "AsignarCategoria";
    public const string GestionarTarifas = "GestionarTarifas";
    public const string GenerarRecibos = "GenerarRecibos";
    public const string AnularRecibos = "AnularRecibos";
    public const string CerrarPeriodos = "CerrarPeriodos";
    public const string VerAuditoria = "VerAuditoria";
    public const string ConfigurarAsada = "ConfigurarAsada";

    public sealed record Permiso(string Nombre, string Modulo, string Accion, IReadOnlyList<string> Roles);

    private static readonly string[] Todos = [NombresRol.Administrador, NombresRol.Operador, NombresRol.Consulta];
    private static readonly string[] AdministradorYOperador = [NombresRol.Administrador, NombresRol.Operador];
    private static readonly string[] SoloAdministrador = [NombresRol.Administrador];

    /// <summary>Roles de una ASADA, en el orden de las columnas de la matriz.</summary>
    public static readonly IReadOnlyList<string> RolesAsada = Todos;

    public static readonly IReadOnlyList<Permiso> Matriz =
    [
        new(Consultar, "General", "Consultar abonados, lecturas, recibos y tarifas; descargar los PDF", Todos),
        new(EditarAbonados, "Abonados", "Crear y editar abonados, propiedades y servicios", AdministradorYOperador),
        new(CambiarEstadoAbonados, "Abonados", "Desactivar o reactivar un abonado", SoloAdministrador),
        new(GestionarMedidores, "Mediciones", "Instalar y retirar medidores", AdministradorYOperador),
        new(RegistrarLecturas, "Mediciones", "Registrar lecturas y anotar lecturas pendientes", AdministradorYOperador),
        new(CorregirLecturas, "Mediciones", "Corregir o anular una lectura (con motivo)", SoloAdministrador),
        new(AsignarCategoria, "Tarifas", "Asignar la categoria tarifaria de un servicio", AdministradorYOperador),
        new(GestionarTarifas, "Tarifas", "Crear categorias y versiones de tarifa", SoloAdministrador),
        new(GenerarRecibos, "Recibos", "Generar los recibos del periodo", AdministradorYOperador),
        new(AnularRecibos, "Recibos", "Anular un recibo emitido (con motivo)", SoloAdministrador),
        new(CerrarPeriodos, "Periodos", "Cerrar y reabrir un periodo (reabrir con motivo)", SoloAdministrador),
        new(VerAuditoria, "Seguridad", "Consultar la bitacora de auditoria", SoloAdministrador),
        new(ConfigurarAsada, "Seguridad", "Datos de la ASADA y umbral de consumo anormal", SoloAdministrador),
    ];

    /// <summary>Registra una politica de autorizacion por cada permiso de la matriz.</summary>
    public static AuthorizationOptions AgregarPoliticasAsada(this AuthorizationOptions opciones)
    {
        foreach (var permiso in Matriz)
            opciones.AddPolicy(permiso.Nombre, politica => politica.RequireRole(permiso.Roles));
        return opciones;
    }
}

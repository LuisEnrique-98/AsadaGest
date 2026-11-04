using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Enums;

namespace Asada.Presentacion.Components.Compartido;

/// <summary>Textos en espanol para mostrar enums y valores del dominio en pantalla.</summary>
public static class Etiquetas
{
    public static string De(TipoIdentificacion tipo) => tipo switch
    {
        TipoIdentificacion.CedulaFisica => "Cedula fisica",
        TipoIdentificacion.CedulaJuridica => "Cedula juridica",
        TipoIdentificacion.Dimex => "DIMEX",
        TipoIdentificacion.Pasaporte => "Pasaporte",
        _ => tipo.ToString(),
    };

    public static string De(EstadoRegistro estado) => estado switch
    {
        EstadoRegistro.Activo => "Activo",
        EstadoRegistro.Inactivo => "Inactivo",
        _ => estado.ToString(),
    };

    public static string ClaseEstado(EstadoRegistro estado)
        => estado == EstadoRegistro.Activo ? "bg-success" : "bg-secondary";

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "setiembre", "octubre", "noviembre", "diciembre",
    ];

    public static string Mes(int mes) => mes is >= 1 and <= 12 ? Meses[mes - 1] : mes.ToString();

    public static string Periodo(int anio, int mes) => $"{Mes(mes)} {anio}";

    public static string Metros(decimal valor) => $"{valor:0.###} m³";

    public static string De(DecisionSeguimientoLectura decision) => decision switch
    {
        DecisionSeguimientoLectura.LeerLoAntesPosible => "Leer lo antes posible",
        DecisionSeguimientoLectura.EsperarAlSiguienteMes => "Esperar al mes siguiente",
        _ => decision.ToString(),
    };

    public static string De(EstadoVentanaLectura estado) => estado switch
    {
        EstadoVentanaLectura.NoInicia => "Aun no inicia",
        EstadoVentanaLectura.Abierta => "Abierta",
        EstadoVentanaLectura.Cerrada => "Cerrada",
        _ => estado.ToString(),
    };

    public static string ClaseVentana(EstadoVentanaLectura estado) => estado switch
    {
        EstadoVentanaLectura.Abierta => "bg-success",
        EstadoVentanaLectura.Cerrada => "bg-dark",
        _ => "bg-secondary",
    };

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy");

    public static string Colones(decimal monto) => $"₡{monto:N2}";

    public static string De(ModoHidrante modo) => modo switch
    {
        ModoHidrante.NoAplica => "No se cobra",
        ModoHidrante.PorMetroCubico => "Por m³ consumido",
        ModoHidrante.MontoFijo => "Monto fijo por recibo",
        _ => modo.ToString(),
    };

    /// <summary>Tarifa de hidrantes en una linea: "₡12,50 por m³", "₡300,00 por recibo" o "No se cobra".</summary>
    public static string Hidrante(ModoHidrante modo, decimal valor) => modo switch
    {
        ModoHidrante.PorMetroCubico => $"{Precio(valor)} por m³",
        ModoHidrante.MontoFijo => $"{Colones(valor)} por recibo",
        _ => "No se cobra",
    };

    /// <summary>Precio unitario: hasta 4 decimales, sin ceros de mas (₡312,5 -> ₡312,50).</summary>
    public static string Precio(decimal precio)
        => decimal.Round(precio, 2) == precio ? Colones(precio) : $"₡{precio:#,##0.00##}";

    public static string De(EstadoFacturacion estado) => estado switch
    {
        EstadoFacturacion.ConRecibo => "Con recibo",
        EstadoFacturacion.ListoParaFacturar => "Listo para facturar",
        EstadoFacturacion.SinLectura => "Sin lectura",
        EstadoFacturacion.SinCategoria => "Sin categoria",
        EstadoFacturacion.SinTarifa => "Sin tarifa vigente",
        _ => estado.ToString(),
    };

    public static string ClaseFacturacion(EstadoFacturacion estado) => estado switch
    {
        EstadoFacturacion.ConRecibo => "bg-success",
        EstadoFacturacion.ListoParaFacturar => "bg-primary",
        EstadoFacturacion.SinLectura => "bg-secondary",
        _ => "bg-warning text-dark",
    };

    public static string De(EstadoRecibo estado) => estado switch
    {
        EstadoRecibo.Emitido => "Emitido",
        EstadoRecibo.Anulado => "Anulado",
        _ => estado.ToString(),
    };

    // ---- Sprint 5 ----

    private static readonly TimeSpan DesfaseCostaRica = TimeSpan.FromHours(-6);

    /// <summary>Fecha y hora de Costa Rica (UTC-6) de un instante guardado en UTC.</summary>
    public static string FechaHora(DateTime utc) => (utc + DesfaseCostaRica).ToString("dd/MM/yyyy HH:mm");

    public static string De(EstadoPeriodo estado) => estado switch
    {
        EstadoPeriodo.Abierto => "Abierto",
        EstadoPeriodo.Cerrado => "Cerrado",
        _ => estado.ToString(),
    };

    public static string ClasePeriodo(EstadoPeriodo estado)
        => estado == EstadoPeriodo.Cerrado ? "bg-dark" : "bg-success";

    public static string De(AccionAuditoria accion) => accion switch
    {
        AccionAuditoria.Creacion => "Creacion",
        AccionAuditoria.Modificacion => "Modificacion",
        AccionAuditoria.Eliminacion => "Eliminacion",
        AccionAuditoria.InicioSesion => "Inicio de sesion",
        AccionAuditoria.InicioSesionFallido => "Intento fallido",
        AccionAuditoria.CuentaBloqueada => "Cuenta bloqueada",
        _ => accion.ToString(),
    };

    public static string ClaseAccion(AccionAuditoria accion) => accion switch
    {
        AccionAuditoria.Creacion => "bg-success",
        AccionAuditoria.Modificacion => "bg-primary",
        AccionAuditoria.InicioSesion => "bg-secondary",
        AccionAuditoria.InicioSesionFallido => "bg-warning text-dark",
        _ => "bg-danger",
    };

    // ---- Sprint 5b ----

    public static string De(EstadoUsuario estado) => estado switch
    {
        EstadoUsuario.Activo => "Activo",
        EstadoUsuario.Inactivo => "Desactivado",
        _ => estado.ToString(),
    };

    public static string ClaseEstado(EstadoUsuario estado)
        => estado == EstadoUsuario.Activo ? "bg-success" : "bg-secondary";

    public static string De(EstadoOrganizacion estado) => estado switch
    {
        EstadoOrganizacion.Activa => "Activa",
        EstadoOrganizacion.Inactiva => "Inactiva",
        _ => estado.ToString(),
    };

    /// <summary>Hora de Costa Rica de un instante UTC con desfase (p. ej. el fin de un bloqueo).</summary>
    public static string FechaHora(DateTimeOffset instante) => FechaHora(instante.UtcDateTime);

    /// <summary>Que puede hacer cada rol, en una frase (formularios de usuarios).</summary>
    public static string DescripcionRol(string rol) => rol switch
    {
        "Administrador" => "Todo, incluidos usuarios, tarifas, anulaciones, cierre de periodos y auditoria.",
        "Operador" => "Trabajo diario: abonados, medidores, lecturas y generacion de recibos.",
        "Consulta" => "Solo ver informacion y descargar recibos.",
        _ => "",
    };

    /// <summary>Nombre en pantalla de cada entidad de la bitacora.</summary>
    public static string Entidad(string entidad) => entidad switch
    {
        "Medicion" => "Lectura",
        "SeguimientoLectura" => "Anotacion de lectura",
        "CategoriaServicio" => "Categoria",
        "Organizacion" => "ASADA",
        "Usuario" => "Usuario",
        _ => entidad,
    };

    /// <summary>"MotivoAnulacion" -> "Motivo anulacion": los nombres de campo ya estan en espanol.</summary>
    public static string Campo(string campo)
    {
        if (string.IsNullOrEmpty(campo)) return campo;
        var texto = System.Text.RegularExpressions.Regex.Replace(campo, "(?<=[a-z0-9])([A-Z])", " $1").ToLowerInvariant();
        return char.ToUpperInvariant(texto[0]) + texto[1..];
    }
}

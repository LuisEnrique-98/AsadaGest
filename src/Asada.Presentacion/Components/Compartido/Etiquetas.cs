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
}

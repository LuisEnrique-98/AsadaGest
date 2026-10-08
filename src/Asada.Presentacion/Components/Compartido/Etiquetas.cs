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

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy");

    public static string Colones(decimal monto) => $"₡{monto:N2}";
}

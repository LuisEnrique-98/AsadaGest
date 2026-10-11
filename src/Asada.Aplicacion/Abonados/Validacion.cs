using System.Net.Mail;

namespace Asada.Aplicacion.Abonados;

/// <summary>Helpers de validacion compartidos por los casos de uso de Abonados. Los largos
/// maximos coinciden con las configuraciones de EF Core en Asada.Infraestructura.</summary>
internal static class Validacion
{
    public static string? Limpiar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public static void Largo(List<string> errores, string? valor, int max, string campo)
    {
        if (valor is not null && valor.Length > max)
            errores.Add($"{campo} no puede superar {max} caracteres.");
    }

    public static void Requerido(List<string> errores, string? valor, int max, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) { errores.Add($"{campo} es obligatorio."); return; }
        Largo(errores, valor.Trim(), max, campo);
    }

    public static bool CorreoValido(string correo)
        => MailAddress.TryCreate(correo, out var m) && m.Address == correo;
}

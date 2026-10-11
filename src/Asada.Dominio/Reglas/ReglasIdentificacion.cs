using Asada.Dominio.Enums;

namespace Asada.Dominio.Reglas;

/// <summary>
/// Normalizacion y validacion de numeros de identificacion segun su tipo.
/// Formatos costarricenses habituales (se guardan normalizados, sin guiones ni espacios):
///   Cedula fisica   : 9 digitos   (1-0234-0567  -> 102340567)
///   Cedula juridica : 10 digitos  (3-101-123456 -> 3101123456)
///   DIMEX           : 11 o 12 digitos
///   Pasaporte       : 5 a 20 caracteres alfanumericos
/// </summary>
public static class ReglasIdentificacion
{
    public static string Normalizar(TipoIdentificacion tipo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

        var caracteres = valor.Where(c => char.IsLetterOrDigit(c));
        var texto = new string(caracteres.ToArray());

        return tipo == TipoIdentificacion.Pasaporte ? texto.ToUpperInvariant() : texto;
    }

    public static bool EsValida(TipoIdentificacion tipo, string? valor)
    {
        var n = Normalizar(tipo, valor);
        if (n.Length == 0) return false;

        return tipo switch
        {
            TipoIdentificacion.CedulaFisica => n.Length == 9 && SoloDigitos(n),
            TipoIdentificacion.CedulaJuridica => n.Length == 10 && SoloDigitos(n),
            TipoIdentificacion.Dimex => n.Length is 11 or 12 && SoloDigitos(n),
            TipoIdentificacion.Pasaporte => n.Length is >= 5 and <= 20,
            _ => false,
        };
    }

    public static string MensajeFormato(TipoIdentificacion tipo) => tipo switch
    {
        TipoIdentificacion.CedulaFisica => "La cedula fisica debe tener 9 digitos (ej. 1-0234-0567).",
        TipoIdentificacion.CedulaJuridica => "La cedula juridica debe tener 10 digitos (ej. 3-101-123456).",
        TipoIdentificacion.Dimex => "El DIMEX debe tener 11 o 12 digitos.",
        TipoIdentificacion.Pasaporte => "El pasaporte debe tener entre 5 y 20 letras o digitos.",
        _ => "Tipo de identificacion no reconocido.",
    };

    private static bool SoloDigitos(string texto) => texto.All(char.IsAsciiDigit);
}

namespace Asada.Dominio.Reglas;

/// <summary>
/// Politica de contrasenas de la plataforma (correccion C-07 del SRS, &#167;24; decision del usuario del
/// 4 de noviembre de 2026): al menos 10 caracteres, con al menos una letra y un numero. No vence: la
/// recomendacion actual (NIST SP 800-63B) es no obligar a cambiarla cada cierto tiempo, porque eso
/// lleva a contrasenas predecibles. Se cambia solo si el usuario quiere, o si el Administrador la
/// restablece (entonces el usuario recibe una temporal y debe cambiarla al entrar).
///
/// La aplican los casos de uso (para mostrar los mensajes en pantalla) y, como segunda red, el
/// validador de ASP.NET Core Identity en Asada.Infraestructura.
/// </summary>
public static class ReglasContrasena
{
    public const int LargoMinimo = 10;

    /// <summary>Tope razonable para que nadie envie textos enormes al calculo del hash.</summary>
    public const int LargoMaximo = 128;

    public static string Descripcion =>
        $"Al menos {LargoMinimo} caracteres, con al menos una letra y un numero.";

    /// <summary>Errores de la contrasena (lista vacia si cumple la politica).</summary>
    /// <param name="correo">Correo del usuario: la contrasena no puede ser el correo ni contenerlo.</param>
    public static IReadOnlyList<string> Validar(string? contrasena, string? correo = null)
    {
        var errores = new List<string>();
        if (string.IsNullOrEmpty(contrasena))
        {
            errores.Add("Indique la contrasena.");
            return errores;
        }

        if (contrasena.Length < LargoMinimo)
            errores.Add($"La contrasena debe tener al menos {LargoMinimo} caracteres.");
        if (contrasena.Length > LargoMaximo)
            errores.Add($"La contrasena no puede superar {LargoMaximo} caracteres.");
        if (!contrasena.Any(char.IsLetter))
            errores.Add("La contrasena debe incluir al menos una letra.");
        if (!contrasena.Any(char.IsDigit))
            errores.Add("La contrasena debe incluir al menos un numero.");
        if (contrasena.Trim().Length != contrasena.Length)
            errores.Add("La contrasena no puede empezar ni terminar con espacios.");

        if (!string.IsNullOrWhiteSpace(correo))
        {
            var usuarioCorreo = correo.Trim().Split('@')[0];
            if (contrasena.Contains(correo.Trim(), StringComparison.OrdinalIgnoreCase)
                || (usuarioCorreo.Length >= 4 && contrasena.Contains(usuarioCorreo, StringComparison.OrdinalIgnoreCase)))
                errores.Add("La contrasena no puede contener su correo.");
        }

        return errores;
    }

    public static bool EsValida(string? contrasena, string? correo = null) => Validar(contrasena, correo).Count == 0;
}

namespace Asada.Dominio.Reglas;

/// <summary>
/// Motivo obligatorio de las acciones sensibles (anular un recibo, corregir o anular una lectura,
/// reabrir un periodo). Queda guardado en el registro y en la auditoria, asi que se pide un texto
/// que explique algo, no solo "error".
/// </summary>
public static class ReglasMotivo
{
    public const int LargoMinimo = 10;
    public const int LargoMaximo = 400;

    /// <summary>Devuelve el mensaje de error, o null si el motivo es valido.</summary>
    public static string? Validar(string? motivo)
    {
        var texto = motivo?.Trim() ?? "";
        if (texto.Length == 0) return "Indique el motivo.";
        if (texto.Length < LargoMinimo) return $"El motivo debe explicar lo ocurrido (al menos {LargoMinimo} caracteres).";
        if (texto.Length > LargoMaximo) return $"El motivo no puede superar {LargoMaximo} caracteres.";
        return null;
    }
}

namespace Asada.Dominio.Reglas;

/// <summary>
/// Reglas para registrar una ASADA nueva en la plataforma (Sprint 5b, lo hace el Superadministrador).
/// El codigo es el prefijo de todos sus numeros de abonado y de recibo (CB-AB-000125,
/// CB-REC-2026-000125), por eso es corto, solo letras mayusculas, unico y no se cambia nunca.
/// </summary>
public static class ReglasOrganizacion
{
    public const int LargoMinimoCodigo = 2;
    public const int LargoMaximoCodigo = 5;
    public const int LargoMaximoNombre = 150;

    /// <summary>Codigo sin espacios y en mayusculas ("cb " -> "CB").</summary>
    public static string NormalizarCodigo(string? codigo) => (codigo ?? "").Trim().ToUpperInvariant();

    /// <summary>Mensaje de error del codigo (ya normalizado), o null si es valido.</summary>
    public static string? ValidarCodigo(string codigoNormalizado)
    {
        if (codigoNormalizado.Length == 0) return "Indique el codigo de la ASADA.";
        if (codigoNormalizado.Length < LargoMinimoCodigo || codigoNormalizado.Length > LargoMaximoCodigo
            || !codigoNormalizado.All(c => c is >= 'A' and <= 'Z'))
            return $"El codigo debe tener de {LargoMinimoCodigo} a {LargoMaximoCodigo} letras, sin tildes, numeros ni espacios (por ejemplo CB).";
        return null;
    }
}

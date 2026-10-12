namespace Asada.Dominio.Reglas;

public static class ReglasRecibo
{
    /// <summary>
    /// Numero visible del recibo: codigo de la ASADA + "REC" + anio del periodo + correlativo de
    /// 6 digitos, p. ej. "CB-REC-2026-000125" (SRS v1.1). El correlativo es independiente por
    /// organizacion y vuelve a empezar en 1 cada anio.
    /// </summary>
    public static string GenerarNumero(string codigoOrganizacion, int anio, int correlativo)
        => $"{codigoOrganizacion}-REC-{anio}-{correlativo:000000}";
}

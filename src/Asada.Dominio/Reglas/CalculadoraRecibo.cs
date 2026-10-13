using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;

namespace Asada.Dominio.Reglas;

/// <summary>Linea calculada del desglose de un recibo.</summary>
public sealed record LineaRecibo(TipoLineaRecibo Tipo, string Concepto, decimal? Cantidad, decimal? PrecioUnitario, decimal Monto);

public sealed record DesgloseRecibo(IReadOnlyList<LineaRecibo> Lineas, decimal SubtotalAgua, decimal MontoHidrante, decimal Total);

public sealed record ResultadoCalculoRecibo(bool EsValido, DesgloseRecibo? Desglose, string? Error)
{
    public static ResultadoCalculoRecibo Ok(DesgloseRecibo d) => new(true, d, null);
    public static ResultadoCalculoRecibo Falla(string error) => new(false, null, error);
}

/// <summary>
/// Calculo del monto de un recibo a partir de la tarifa y el consumo del periodo.
///
///   1. Cargo fijo: se cobra siempre, aunque el consumo sea 0.
///   2. Consumo escalonado: cada m³ se cobra al precio del tramo en que cae. Con tramos
///      0-15 a 300 y 15-25 a 450, un consumo de 20 m³ es 15 x 300 + 5 x 450 (no 20 x 450).
///      Se genera una linea por tramo con consumo, para que el recibo muestre el detalle.
///   3. Hidrantes: monto por m³ consumido o monto fijo por recibo, segun la tarifa.
///
/// Cada linea se redondea a 2 decimales (centimos, redondeo comercial) y el total es la suma
/// de las lineas ya redondeadas: asi el desglose impreso siempre suma exactamente el total.
///
/// Es una funcion pura, sin base de datos: se prueba con xUnit sin infraestructura.
/// </summary>
public static class CalculadoraRecibo
{
    public static ResultadoCalculoRecibo Calcular(Tarifa tarifa, decimal consumo)
    {
        if (consumo < 0) return ResultadoCalculoRecibo.Falla("El consumo no puede ser negativo.");

        var tramos = tarifa.Bloques.OrderBy(b => b.Orden)
            .Select(b => new TramoTarifa(b.LimiteSuperior, b.PrecioPorMetroCubico)).ToList();
        var errores = ReglasTarifa.Validar(tarifa.CargoFijo, tarifa.ModoHidrante, tarifa.ValorHidrante, tramos);
        if (errores.Count > 0)
            return ResultadoCalculoRecibo.Falla("La tarifa no es valida: " + string.Join(" ", errores));

        var lineas = new List<LineaRecibo>();

        if (tarifa.CargoFijo > 0)
            lineas.Add(new LineaRecibo(TipoLineaRecibo.CargoFijo, "Cargo fijo", null, null, Redondear(tarifa.CargoFijo)));

        decimal desde = 0m;
        foreach (var tramo in tramos)
        {
            var hasta = tramo.LimiteSuperior;
            var metrosEnTramo = hasta is decimal h
                ? Math.Clamp(consumo - desde, 0m, h - desde)
                : Math.Max(consumo - desde, 0m);

            if (metrosEnTramo > 0)
            {
                lineas.Add(new LineaRecibo(
                    TipoLineaRecibo.Consumo,
                    $"Consumo {ReglasTarifa.DescribirTramo(desde, hasta)}",
                    metrosEnTramo,
                    tramo.PrecioPorMetroCubico,
                    Redondear(metrosEnTramo * tramo.PrecioPorMetroCubico)));
            }

            if (hasta is not decimal limite || consumo <= limite) break;
            desde = limite;
        }

        switch (tarifa.ModoHidrante)
        {
            case ModoHidrante.PorMetroCubico when consumo > 0:
                lineas.Add(new LineaRecibo(TipoLineaRecibo.Hidrante, "Tarifa de hidrantes", consumo,
                    tarifa.ValorHidrante, Redondear(consumo * tarifa.ValorHidrante)));
                break;
            case ModoHidrante.MontoFijo:
                lineas.Add(new LineaRecibo(TipoLineaRecibo.Hidrante, "Tarifa de hidrantes", null, null,
                    Redondear(tarifa.ValorHidrante)));
                break;
        }

        var subtotalAgua = lineas.Where(l => l.Tipo != TipoLineaRecibo.Hidrante).Sum(l => l.Monto);
        var hidrante = lineas.Where(l => l.Tipo == TipoLineaRecibo.Hidrante).Sum(l => l.Monto);

        return ResultadoCalculoRecibo.Ok(new DesgloseRecibo(lineas, subtotalAgua, hidrante, subtotalAgua + hidrante));
    }

    public static decimal Redondear(decimal monto) => Math.Round(monto, 2, MidpointRounding.AwayFromZero);
}

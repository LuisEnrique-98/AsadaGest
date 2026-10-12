using System.Globalization;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;

namespace Asada.Dominio.Reglas;

/// <summary>Tramo tal como lo escribe el usuario: hasta cuantos m³ llega (null = sin limite) y su precio.</summary>
public sealed record TramoTarifa(decimal? LimiteSuperior, decimal PrecioPorMetroCubico);

/// <summary>
/// Reglas de las tarifas: que una version sea coherente y cual version rige para un periodo.
///
/// Version vigente de un periodo: la ultima version de la categoria cuya fecha "vigente desde"
/// es igual o anterior al ultimo dia del mes del periodo. Se toma el fin de mes porque las
/// lecturas se toman en la ultima semana del mes (decision del 9 de octubre de 2026): una tarifa
/// que entra a regir a mitad de mes ya rige cuando se lee el medidor.
/// </summary>
public static class ReglasTarifa
{
    /// <summary>Tramos con limite que se pueden escribir, sin contar el excedente (el tramo final sin limite).</summary>
    public const int MaximoTramosConLimite = 11;

    /// <summary>Total de tramos de una tarifa: los que tienen limite mas el excedente.</summary>
    public const int MaximoTramos = MaximoTramosConLimite + 1;

    public static IReadOnlyList<string> Validar(
        decimal cargoFijo, ModoHidrante modoHidrante, decimal valorHidrante, IReadOnlyList<TramoTarifa> tramos)
    {
        var errores = new List<string>();

        if (cargoFijo < 0) errores.Add("El cargo fijo no puede ser negativo.");

        if (!Enum.IsDefined(modoHidrante)) errores.Add("La forma de cobro de hidrantes no es valida.");
        else if (modoHidrante == ModoHidrante.NoAplica && valorHidrante != 0)
            errores.Add("Si la tarifa no cobra hidrantes, el monto de hidrantes debe ser 0.");
        else if (modoHidrante != ModoHidrante.NoAplica && valorHidrante <= 0)
            errores.Add("Indique el monto de la tarifa de hidrantes (mayor que 0).");

        if (tramos.Count == 0)
        {
            errores.Add("La tarifa debe tener al menos un tramo de consumo.");
            return errores;
        }
        if (tramos.Count > MaximoTramos) errores.Add($"La tarifa puede tener como maximo {MaximoTramos} tramos.");

        decimal limiteAnterior = 0m;
        for (var i = 0; i < tramos.Count; i++)
        {
            var tramo = tramos[i];
            var numero = i + 1;
            var esUltimo = i == tramos.Count - 1;

            if (tramo.PrecioPorMetroCubico < 0) errores.Add($"El precio del tramo {numero} no puede ser negativo.");

            if (esUltimo)
            {
                if (tramo.LimiteSuperior is not null)
                    errores.Add("El ultimo tramo no debe tener limite: cubre todo el consumo que exceda al tramo anterior.");
            }
            else if (tramo.LimiteSuperior is not decimal limite)
            {
                errores.Add($"Solo el ultimo tramo puede quedar sin limite (revise el tramo {numero}).");
            }
            else if (limite <= limiteAnterior)
            {
                errores.Add(i == 0
                    ? "El limite del primer tramo debe ser mayor que 0 m³."
                    : $"El limite del tramo {numero} debe ser mayor que el del tramo {numero - 1}.");
            }
            else
            {
                limiteAnterior = limite;
            }
        }

        return errores;
    }

    /// <summary>Ultimo dia del mes del periodo: la fecha con que se elige la version de tarifa.</summary>
    public static DateOnly FechaReferencia(int anio, int mes)
        => new(anio, mes, DateTime.DaysInMonth(anio, mes));

    /// <summary>Version que rige en la fecha indicada, o null si ninguna version habia entrado a regir.</summary>
    public static Tarifa? SeleccionarVigente(IEnumerable<Tarifa> versionesDeLaCategoria, DateOnly fecha)
        => versionesDeLaCategoria
            .Where(t => t.VigenteDesde <= fecha)
            .OrderByDescending(t => t.VigenteDesde)
            .ThenByDescending(t => t.Id)
            .FirstOrDefault();

    /// <summary>Texto del tramo para pantallas y recibos: "0 a 15 m³", "mas de 15 a 25 m³", "mas de 120 m³".</summary>
    public static string DescribirTramo(decimal desde, decimal? hasta)
    {
        if (hasta is not decimal h) return $"mas de {M3(desde)} m³";
        return desde == 0 ? $"0 a {M3(h)} m³" : $"mas de {M3(desde)} a {M3(h)} m³";
    }

    internal static string M3(decimal valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);
}

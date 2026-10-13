using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Calculo compartido por el registro y la correccion de una lectura: consumo (con la cadena
/// de medidores entre la lectura anterior y esta, RB-013) y motivos de revision manual.
/// </summary>
internal static class EvaluadorLectura
{
    public sealed record Evaluacion(decimal Consumo, IReadOnlyList<string> MotivosRevision);

    /// <param name="medidorLectura">Medidor con el que se tomo esta lectura.</param>
    /// <param name="medidores">Todos los medidores del servicio, en orden de instalacion.</param>
    /// <param name="anterior">Lectura vigente previa del servicio (de un periodo anterior), o null.</param>
    /// <param name="consumosPrevios">Consumos previos, del mas reciente al mas antiguo.</param>
    public static Resultado<Evaluacion> Evaluar(
        int anio, int mes, DateOnly fechaLectura, decimal lectura,
        Medidor medidorLectura, IReadOnlyList<Medidor> medidores, LecturaAnterior? anterior,
        IReadOnlyList<decimal> consumosPrevios, decimal porcentajeUmbral)
    {
        if (fechaLectura < medidorLectura.FechaInstalacion)
            return Resultado<Evaluacion>.Falla(
                $"La fecha de lectura es anterior a la instalacion del medidor ({medidorLectura.FechaInstalacion:dd/MM/yyyy}).");

        if (anterior is not null && fechaLectura < anterior.FechaLectura)
            return Resultado<Evaluacion>.Falla(
                $"La fecha de lectura es anterior a la lectura previa ({anterior.FechaLectura:dd/MM/yyyy}).");

        // Cadena de medidores entre la lectura anterior y esta (RB-013).
        IReadOnlyList<Medidor> cadena;
        if (anterior is null)
        {
            cadena = [medidorLectura];
        }
        else
        {
            var lista = medidores.ToList();
            var indiceAnterior = lista.FindIndex(m => m.Id == anterior.MedidorId);
            var indiceActual = lista.FindIndex(m => m.Id == medidorLectura.Id);
            if (indiceAnterior < 0 || indiceActual < indiceAnterior)
                return Resultado<Evaluacion>.Falla("El historial de medidores del servicio es inconsistente.");

            cadena = lista.Skip(indiceAnterior).Take(indiceActual - indiceAnterior + 1).ToList();
        }

        var consumo = CalculadoraConsumo.Calcular(anterior, cadena, lectura);
        if (!consumo.EsValido) return Resultado<Evaluacion>.Falla(consumo.Error!);

        var motivos = new List<string>();

        if (!ReglasPeriodo.EstaEnVentana(anio, mes, fechaLectura))
            motivos.Add(
                $"Lectura fuera de la ventana del periodo ({ReglasPeriodo.InicioVentana(anio, mes):dd/MM} al {ReglasPeriodo.FinVentana(anio, mes):dd/MM/yyyy})");

        if (anterior is not null)
        {
            var (anioPrevio, mesPrevio) = ReglasPeriodo.Anterior(anio, mes);
            if (ReglasPeriodo.Comparar(anterior.Anio, anterior.Mes, anioPrevio, mesPrevio) < 0)
                motivos.Add("El periodo anterior quedo sin lectura; el consumo incluye mas de un mes");
        }

        if (ReglasConsumoAnormal.EsAnormal(consumo.Consumo, consumosPrevios, porcentajeUmbral))
            motivos.Add("Consumo muy superior al promedio de los ultimos periodos");

        return Resultado<Evaluacion>.Ok(new Evaluacion(consumo.Consumo, motivos));
    }
}

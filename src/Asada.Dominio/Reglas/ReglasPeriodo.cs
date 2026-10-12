using Asada.Dominio.Enums;

namespace Asada.Dominio.Reglas;

/// <summary>
/// Ventana de toma de lecturas: la ultima semana de cada mes (decision de negocio del
/// 9 de octubre de 2026). Fuera de esa ventana el sistema sabe que "ya toco leer" o "todavia
/// no toca", y puede alertar de los servicios sin lectura. Por ahora es una regla fija; si cada
/// ASADA llegara a necesitar otra ventana, se mueve a un parametro de la organizacion.
/// </summary>
public static class ReglasPeriodo
{
    public const int DiasVentanaLectura = 7;

    public static DateOnly FinVentana(int anio, int mes)
        => new(anio, mes, DateTime.DaysInMonth(anio, mes));

    public static DateOnly InicioVentana(int anio, int mes)
        => FinVentana(anio, mes).AddDays(-(DiasVentanaLectura - 1));

    public static bool EstaEnVentana(int anio, int mes, DateOnly fecha)
        => fecha >= InicioVentana(anio, mes) && fecha <= FinVentana(anio, mes);

    public static EstadoVentanaLectura EstadoVentana(int anio, int mes, DateOnly hoy)
    {
        if (hoy < InicioVentana(anio, mes)) return EstadoVentanaLectura.NoInicia;
        return hoy <= FinVentana(anio, mes) ? EstadoVentanaLectura.Abierta : EstadoVentanaLectura.Cerrada;
    }

    public static (int Anio, int Mes) Anterior(int anio, int mes)
        => mes == 1 ? (anio - 1, 12) : (anio, mes - 1);

    public static (int Anio, int Mes) Siguiente(int anio, int mes)
        => mes == 12 ? (anio + 1, 1) : (anio, mes + 1);

    /// <summary>
    /// Periodo que conviene mostrar por defecto: el del mes actual si ya empezo su ventana de
    /// lectura; si no, el del mes anterior (cuyas lecturas pueden estar pendientes).
    /// </summary>
    public static (int Anio, int Mes) PeriodoPorDefecto(DateOnly hoy)
        => hoy >= InicioVentana(hoy.Year, hoy.Month) ? (hoy.Year, hoy.Month) : Anterior(hoy.Year, hoy.Month);

    public static bool EsMesValido(int anio, int mes) => anio is >= 2000 and <= 2100 && mes is >= 1 and <= 12;

    /// <summary>Compara dos periodos: negativo si (a) es anterior a (b).</summary>
    public static int Comparar(int anioA, int mesA, int anioB, int mesB)
        => anioA != anioB ? anioA.CompareTo(anioB) : mesA.CompareTo(mesB);
}

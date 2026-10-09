namespace Asada.Dominio.Enums;

public enum EstadoVentanaLectura
{
    /// <summary>Todavia no llega la ultima semana del mes.</summary>
    NoInicia = 1,
    /// <summary>Hoy esta dentro de la ventana de toma de lecturas.</summary>
    Abierta = 2,
    /// <summary>La ventana ya paso: los servicios sin lectura generan alerta.</summary>
    Cerrada = 3,
}

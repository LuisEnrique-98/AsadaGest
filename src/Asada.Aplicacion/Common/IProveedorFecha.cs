namespace Asada.Aplicacion.Common;

/// <summary>
/// Fecha de "hoy" en la zona horaria de la ASADA (Costa Rica, UTC-6 sin horario de verano).
/// Existe para no depender de la zona horaria del servidor (un VPS suele estar en UTC, y a las
/// 6 pm hora local ya seria "manana") y para poder fijar la fecha en las pruebas.
/// </summary>
public interface IProveedorFecha
{
    DateOnly Hoy { get; }
}

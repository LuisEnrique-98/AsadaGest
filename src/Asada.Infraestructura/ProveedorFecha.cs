using Asada.Aplicacion.Common;

namespace Asada.Infraestructura;

/// <summary>
/// "Hoy" en hora de Costa Rica (UTC-6 todo el ano, sin horario de verano). Se calcula con un
/// desfase fijo en vez de TimeZoneInfo para que funcione igual en Windows y en Linux, y no
/// dependa de la zona horaria configurada en el servidor.
/// </summary>
public class ProveedorFecha : IProveedorFecha
{
    private static readonly TimeSpan DesfaseCostaRica = TimeSpan.FromHours(-6);

    public DateOnly Hoy => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(DesfaseCostaRica).DateTime);
}

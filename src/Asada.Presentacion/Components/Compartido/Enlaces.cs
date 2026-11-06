namespace Asada.Presentacion.Components.Compartido;

/// <summary>Enlaces que deben conservar la ASADA elegida por el Superadministrador (?asada=N).</summary>
public static class Enlaces
{
    public static string Usuarios(string ruta, int? asada)
        => asada is int id ? $"{ruta}{(ruta.Contains('?') ? '&' : '?')}asada={id}" : ruta;
}

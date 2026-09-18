namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Nombres de rol reconocidos por la plataforma (SRS &#167;23, matriz de permisos).
/// Centralizados aqui para no repetir strings literales al sembrar datos o al
/// escribir politicas de autorizacion en Sprint 5.
/// </summary>
public static class NombresRol
{
    public const string Superadministrador = "Superadministrador";
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Consulta = "Consulta";

    public static readonly IReadOnlyList<string> Todos =
    [
        Superadministrador,
        Administrador,
        Operador,
        Consulta,
    ];
}

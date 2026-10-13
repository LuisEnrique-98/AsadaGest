using System.Security.Claims;
using Asada.Aplicacion.Common;
using Microsoft.AspNetCore.Http;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Implementacion de IProveedorUsuario: lee el id (NameIdentifier) y el nombre (GivenName,
/// agregado por UsuarioClaimsPrincipalFactory) del usuario autenticado de la solicitud HTTP.
/// Sin usuario autenticado (p. ej. los datos iniciales al arrancar) devuelve "Sistema".
/// </summary>
public class ProveedorUsuario(IHttpContextAccessor httpContextAccessor) : IProveedorUsuario
{
    public const string NombreSistema = "Sistema";

    private ClaimsPrincipal? Usuario =>
        httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } u ? u : null;

    public int? UsuarioId
        => int.TryParse(Usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string Nombre
        => Usuario?.FindFirst(ClaimTypes.GivenName)?.Value
           ?? Usuario?.Identity?.Name
           ?? NombreSistema;
}

using Asada.Dominio.Reglas;
using Microsoft.AspNetCore.Identity;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Aplica la politica de contrasenas de la plataforma (ReglasContrasena: 10+ caracteres con letras y
/// numeros, sin el correo) dentro de ASP.NET Core Identity. Los casos de uso ya la validan antes,
/// para mostrar los mensajes en pantalla; esto es la segunda red, por si algun camino futuro llama a
/// UserManager directamente (p. ej. los datos iniciales).
/// </summary>
public sealed class ValidadorContrasenaAsada : IPasswordValidator<Usuario>
{
    public Task<IdentityResult> ValidateAsync(UserManager<Usuario> manager, Usuario user, string? password)
    {
        var errores = ReglasContrasena.Validar(password, user.Email);
        return Task.FromResult(errores.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errores.Select(e => new IdentityError { Code = "PoliticaContrasena", Description = e }).ToArray()));
    }
}

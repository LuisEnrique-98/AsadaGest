using Microsoft.AspNetCore.Identity;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Mensajes de ASP.NET Core Identity en espanol (sin tildes, como el resto de la interfaz). Solo se
/// traducen los que pueden llegar a la pantalla con la forma en que el sistema usa Identity.
/// </summary>
public sealed class DescriptorErroresIdentidad : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => new() { Code = nameof(DefaultError), Description = "Ocurrio un error al guardar el usuario." };

    public override IdentityError ConcurrencyFailure()
        => new() { Code = nameof(ConcurrencyFailure), Description = "Otra persona modifico este usuario al mismo tiempo. Recargue la pagina e intente de nuevo." };

    public override IdentityError PasswordMismatch()
        => new() { Code = nameof(PasswordMismatch), Description = "La contrasena actual no es correcta." };

    public override IdentityError InvalidToken()
        => new() { Code = nameof(InvalidToken), Description = "El enlace o codigo ya no es valido." };

    public override IdentityError InvalidEmail(string? email)
        => new() { Code = nameof(InvalidEmail), Description = "El correo no es valido." };

    public override IdentityError InvalidUserName(string? userName)
        => new() { Code = nameof(InvalidUserName), Description = "El correo no es valido como usuario de acceso." };

    public override IdentityError DuplicateEmail(string email)
        => new() { Code = nameof(DuplicateEmail), Description = "Ya existe un usuario con ese correo." };

    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = nameof(DuplicateUserName), Description = "Ya existe un usuario con ese correo." };

    public override IdentityError PasswordTooShort(int length)
        => new() { Code = nameof(PasswordTooShort), Description = $"La contrasena debe tener al menos {length} caracteres." };

    public override IdentityError UserAlreadyInRole(string role)
        => new() { Code = nameof(UserAlreadyInRole), Description = $"El usuario ya tiene el rol {role}." };

    public override IdentityError UserNotInRole(string role)
        => new() { Code = nameof(UserNotInRole), Description = $"El usuario no tiene el rol {role}." };

    public override IdentityError UserAlreadyHasPassword()
        => new() { Code = nameof(UserAlreadyHasPassword), Description = "El usuario ya tiene contrasena." };

    public override IdentityError UserLockoutNotEnabled()
        => new() { Code = nameof(UserLockoutNotEnabled), Description = "El bloqueo por intentos no esta habilitado para este usuario." };
}

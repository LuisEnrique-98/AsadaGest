using System.Security.Cryptography;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Usuarios;

/// <summary>
/// Contrasena temporal aleatoria de 12 caracteres, en grupos faciles de dictar o copiar
/// (p. ej. "kp7m-x3rw-9tqa"). Usa letras minusculas y numeros sin los que se confunden
/// (0/o, 1/l/i) y siempre trae al menos una letra y un numero, asi que cumple ReglasContrasena.
/// Se muestra una sola vez al Administrador; el usuario la cambia en su primer ingreso.
/// </summary>
public sealed class GeneradorContrasena : IGeneradorContrasena
{
    private const string Letras = "abcdefghjkmnpqrstuvwxyz";
    private const string Numeros = "23456789";
    private const string Todos = Letras + Numeros;

    public string GenerarTemporal()
    {
        while (true)
        {
            var c = new char[12];
            for (var i = 0; i < c.Length; i++) c[i] = Todos[RandomNumberGenerator.GetInt32(Todos.Length)];
            var texto = $"{new string(c, 0, 4)}-{new string(c, 4, 4)}-{new string(c, 8, 4)}";
            if (ReglasContrasena.EsValida(texto)) return texto;
        }
    }
}

using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioAbonados(AsadaDbContext db) : IRepositorioAbonados
{
    public Task<bool> ExisteIdentificacionAsync(
        TipoIdentificacion tipo, string identificacionNormalizada, CancellationToken ct = default)
        => db.Abonados.AnyAsync(
            a => a.TipoIdentificacion == tipo && a.Identificacion == identificacionNormalizada, ct);

    public async Task AgregarAsync(Abonado abonado, CancellationToken ct = default)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Incremento atomico del contador de la organizacion: UPDATE ... RETURNING toma un
        // bloqueo de fila, asi que dos usuarios creando abonados a la vez nunca reciben el mismo
        // correlativo. Si algo falla mas abajo, la transaccion se revierte y el numero no se "quema".
        // Es SQL directo (no pasa por el filtro global), por eso filtra explicitamente por id.
        var conexion = db.Database.GetDbConnection();
        await using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion.GetDbTransaction();
        comando.CommandText =
            "UPDATE organizaciones " +
            "SET ultimo_correlativo_abonado = ultimo_correlativo_abonado + 1 " +
            "WHERE id = @id " +
            "RETURNING ultimo_correlativo_abonado, codigo";
        comando.Parameters.Add(new NpgsqlParameter("id", abonado.OrganizacionId));

        int correlativo;
        string codigoOrganizacion;
        await using (var lector = await comando.ExecuteReaderAsync(ct))
        {
            if (!await lector.ReadAsync(ct))
                throw new InvalidOperationException(
                    $"La organizacion {abonado.OrganizacionId} no existe; no se pudo asignar correlativo.");

            correlativo = lector.GetInt32(0);
            codigoOrganizacion = lector.GetString(1);
        }

        abonado.Correlativo = correlativo;
        abonado.Codigo = Abonado.GenerarCodigo(codigoOrganizacion, correlativo);

        db.Abonados.Add(abonado);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }

    public Task<Abonado?> ObtenerConExpedienteAsync(int abonadoId, CancellationToken ct = default)
        => db.Abonados
            .Include(a => a.Propiedades)
                .ThenInclude(p => p.Servicios)
            .FirstOrDefaultAsync(a => a.Id == abonadoId, ct);

    public async Task<PaginaResultado<AbonadoResumenDto>> BuscarAsync(
        string? texto, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        IQueryable<Abonado> consulta = db.Abonados.AsNoTracking();

        if (texto is not null)
        {
            // La identificacion se guarda sin guiones: "1-0234-0567" debe encontrar 102340567.
            var textoIdentificacion = new string(texto.Where(char.IsLetterOrDigit).ToArray());
            var buscarIdentificacion = textoIdentificacion.Length > 0;
            var patron = $"%{EscaparLike(texto)}%";
            var patronIdentificacion = $"%{EscaparLike(textoIdentificacion)}%";

            consulta = consulta.Where(a =>
                EF.Functions.ILike(a.Nombre, patron) ||
                EF.Functions.ILike(a.Codigo, patron) ||
                (buscarIdentificacion && EF.Functions.ILike(a.Identificacion, patronIdentificacion)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(a => a.Nombre).ThenBy(a => a.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(a => new AbonadoResumenDto(
                a.Id, a.Codigo, a.Nombre, a.TipoIdentificacion, a.Identificacion,
                a.Telefono, a.Estado,
                a.Propiedades.SelectMany(p => p.Servicios).Count()))
            .ToListAsync(ct);

        return new PaginaResultado<AbonadoResumenDto>(items, total, pagina, tamanoPagina);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static string EscaparLike(string texto)
        => texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

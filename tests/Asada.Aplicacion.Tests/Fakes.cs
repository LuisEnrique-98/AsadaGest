using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Tests;

public sealed class ProveedorOrganizacionFalso(int? organizacionId) : IProveedorOrganizacion
{
    public int? OrganizacionId => organizacionId;
    public bool EsSuperadministrador => organizacionId is null;
}

/// <summary>Repositorio en memoria: reemplaza a RepositorioAbonados (EF Core) en las pruebas de casos de uso.</summary>
public sealed class RepositorioAbonadosEnMemoria : IRepositorioAbonados
{
    public List<Abonado> Abonados { get; } = [];
    public int Guardados { get; private set; }

    public Task<bool> ExisteIdentificacionAsync(TipoIdentificacion tipo, string identificacionNormalizada, CancellationToken ct = default)
        => Task.FromResult(Abonados.Any(a => a.TipoIdentificacion == tipo && a.Identificacion == identificacionNormalizada));

    public Task AgregarAsync(Abonado abonado, CancellationToken ct = default)
    {
        abonado.Id = Abonados.Count + 1;
        abonado.Correlativo = Abonados.Count + 1;
        abonado.Codigo = Abonado.GenerarCodigo("CB", abonado.Correlativo);
        Abonados.Add(abonado);
        return Task.CompletedTask;
    }

    public Task<Abonado?> ObtenerConExpedienteAsync(int abonadoId, CancellationToken ct = default)
        => Task.FromResult(Abonados.FirstOrDefault(a => a.Id == abonadoId));

    public int UltimaPaginaPedida { get; private set; }
    public int UltimoTamanoPedido { get; private set; }

    public Task<PaginaResultado<AbonadoResumenDto>> BuscarAsync(string? texto, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        UltimaPaginaPedida = pagina;
        UltimoTamanoPedido = tamanoPagina;
        return Task.FromResult(new PaginaResultado<AbonadoResumenDto>([], 0, pagina, tamanoPagina));
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        Guardados++;
        return Task.CompletedTask;
    }
}

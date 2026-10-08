namespace Asada.Aplicacion.Common;

public sealed record PaginaResultado<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina)
{
    public int TotalPaginas => TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);
    public bool TieneAnterior => Pagina > 1;
    public bool TieneSiguiente => Pagina < TotalPaginas;
}

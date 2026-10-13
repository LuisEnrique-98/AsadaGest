namespace Asada.Aplicacion.Recibos.Dtos;

public sealed record GenerarRecibosSolicitud(int Anio, int Mes, DateOnly FechaVencimiento);

public sealed record AnularReciboSolicitud(int ReciboId, string Motivo);

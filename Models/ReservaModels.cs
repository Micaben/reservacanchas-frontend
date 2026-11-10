using System.Globalization;

namespace BlazorApp1.Models;

public class DeporteDto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class ClienteDto
{
    public long Id { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
}

public class CanchaDto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioHora { get; set; }
    public int Capacidad { get; set; }
    public string? Estado { get; set; }
    public List<DeporteDto>? Deportes { get; set; }
}

public class ReservaDto
{
    public long Id { get; set; }
    public string Fecha { get; set; } = "";
    public string HoraInicio { get; set; } = "";
    public string HoraFin { get; set; } = "";
    public decimal PrecioTotal { get; set; }
    public string? EstadoReserva { get; set; }
    public CanchaDto? Cancha { get; set; }
    public ClienteDto? Cliente { get; set; }
}

public static class Disponibilidad
{
    // Horas de inicio de cada turno de 1 hora (16:00 a 22:00)
    public static readonly string[] Horas = { "16:00", "17:00", "18:00", "19:00", "20:00", "21:00" };

    // L libre, R reservada, P pendiente, N no disponible, X hora pasada
    public static string Estado(CanchaDto c, string hora, DateOnly fecha, IEnumerable<ReservaDto> reservasDelDia)
    {
        if (c.Estado is "MANTENIMIENTO" or "INACTIVA") return "N";

        var r = reservasDelDia.FirstOrDefault(x =>
            x.Cancha?.Id == c.Id && x.HoraInicio == hora && x.EstadoReserva != "CANCELADA");
        if (r is not null) return r.EstadoReserva == "PENDIENTE" ? "P" : "R";

        if (fecha == DateOnly.FromDateTime(DateTime.Today)
            && TimeOnly.ParseExact(hora, "HH:mm", CultureInfo.InvariantCulture) <= TimeOnly.FromDateTime(DateTime.Now))
            return "X";

        return "L";
    }

    public static string Texto(string e) => e switch
    {
        "R" => "Reservada",
        "P" => "Pendiente",
        "N" => "No disponible",
        "X" => "Pasada",
        _ => "Libre"
    };

    public static string Clase(string e) => e switch
    {
        "R" => "res",
        "P" => "pen",
        "N" or "X" => "nd",
        _ => "lib"
    };
}
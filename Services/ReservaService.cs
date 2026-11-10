using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorApp1.Models;

namespace BlazorApp1.Services;

public class ReservaService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public ReservaService(IHttpClientFactory factory, AuthService auth)
    {
        _http = factory.CreateClient("jhipster");
        _auth = auth;
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, object? cuerpo = null)
    {
        using var req = new HttpRequestMessage(metodo, url);
        var token = await _auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null)
            req.Content = JsonContent.Create(cuerpo);
        return await _http.SendAsync(req);
    }

    private async Task<List<T>> ListarAsync<T>(string url)
    {
        using var resp = await EnviarAsync(HttpMethod.Get, url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<T>>() ?? new();
    }

    public Task<List<CanchaDto>> GetCanchasAsync() => ListarAsync<CanchaDto>("api/canchas?size=200");
    public Task<List<DeporteDto>> GetDeportesAsync() => ListarAsync<DeporteDto>("api/deportes?size=200");
    public Task<List<ReservaDto>> GetReservasAsync() => ListarAsync<ReservaDto>("api/reservas?size=1000");

    public async Task<ClienteDto?> GetMiClienteAsync()
    {
        using var resp = await EnviarAsync(HttpMethod.Get, "api/clientes/me");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ClienteDto>();
    }

    public async Task<(bool Ok, string Mensaje)> CrearReservaAsync(
        CanchaDto cancha, long clienteId, DateOnly fecha, string horaInicio)
    {
        try
        {
            var inicio = TimeOnly.ParseExact(horaInicio, "HH:mm", CultureInfo.InvariantCulture);
            var codigo = "RES-" + DateTime.UtcNow.ToString("yyMMdd") + "-"
                         + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            using var resp = await EnviarAsync(HttpMethod.Post, "api/reservas", new
            {
                codigoReserva = codigo,
                fecha = fecha.ToString("yyyy-MM-dd"),
                horaInicio,
                horaFin = inicio.AddHours(1).ToString("HH:mm"),
                precioTotal = cancha.PrecioHora,
                estadoReserva = "PENDIENTE",
                fechaCreacion = DateTime.UtcNow,
                cancha = new { id = cancha.Id },
                cliente = new { id = clienteId }
            });

            if (resp.IsSuccessStatusCode) return (true, "");

            var body = await resp.Content.ReadAsStringAsync();
            return body.Contains("horarioocupado")
                ? (false, "Ese horario ya fue reservado por otra persona. Elige otro.")
                : (false, $"No se pudo crear la reserva ({(int)resp.StatusCode}).");
        }
        catch (HttpRequestException) { return (false, "No se pudo conectar con el servidor."); }
        catch (TaskCanceledException) { return (false, "El servidor tardó demasiado en responder."); }
    }
}
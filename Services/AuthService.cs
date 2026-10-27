using System.Net;
using System.Net.Http.Json;
using BlazorApp1.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BlazorApp1.Services;

public class AuthService
{
    private const string TokenKey = "jhi_token";
    private readonly HttpClient _http;
    private readonly ProtectedLocalStorage _storage;

    public event Action? AuthChanged;

    public AuthService(IHttpClientFactory factory, ProtectedLocalStorage storage)
    {
        _http = factory.CreateClient("jhipster");
        _storage = storage;
    }

    public async Task<(bool Ok, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/authenticate", request);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return (false, "Usuario o contraseña incorrectos.");
            if (!resp.IsSuccessStatusCode)
                return (false, $"Error del servidor ({(int)resp.StatusCode}).");

            var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
            if (string.IsNullOrEmpty(body?.IdToken))
                return (false, "Respuesta inválida del servidor.");

            await _storage.SetAsync(TokenKey, body.IdToken);
            AuthChanged?.Invoke();
            return (true, null);
        }
        catch (HttpRequestException)
        {
            return (false, "No se pudo conectar con el servidor. ¿Está encendido el backend?");
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var r = await _storage.GetAsync<string>(TokenKey);
            return r.Success ? r.Value : null;
        }
        catch (InvalidOperationException)
        {
            return null; // aún no hay JS disponible
        }
    }

    public async Task LogoutAsync()
    {
        await _storage.DeleteAsync(TokenKey);
        AuthChanged?.Invoke();
    }
}
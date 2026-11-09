using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlazorApp1.Models;

namespace BlazorApp1.Services;

public class AccountService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public AccountService(IHttpClientFactory factory, AuthService auth)
    {
        _http = factory.CreateClient("jhipster");
        _auth = auth;
    }

    public async Task<RegistroResultado> RegisterAsync(RegistroModel m)
    {
        try
        {
            var email = m.Email.Trim().ToLowerInvariant();

            // 1) Crea la cuenta en JHI_USER (el usuario de acceso es el correo)
            var resp = await _http.PostAsJsonAsync("api/register", new
            {
                login = email,
                email,
                password = m.Password,
                firstName = m.Nombres.Trim(),
                lastName = m.Apellidos.Trim(),
                langKey = "es"
            });
            if (!resp.IsSuccessStatusCode)
                return new(false, false, await LeerErrorAsync(resp));

            // 2) Inicia sesión (falla si la cuenta aún no está activada)
            var (ok, _) = await _auth.LoginAsync(new LoginRequest { Username = email, Password = m.Password });
            if (!ok)
                return new(true, false, "Cuenta creada. Está pendiente de activación; cuando esté activa podrás iniciar sesión.");

            var token = await _auth.GetTokenAsync();

            // 3) Lee el id del usuario recién creado
            using var rCuenta = await _http.SendAsync(Crear(HttpMethod.Get, "api/account", token));
            if (!rCuenta.IsSuccessStatusCode)
                return new(false, true, "Se creó tu cuenta, pero no se pudo leer tu perfil.");
            var cuenta = await rCuenta.Content.ReadFromJsonAsync<JsonElement>();
            var userId = cuenta.GetProperty("id").GetInt64();

            // 4) Crea la ficha en CLIENTE enlazada al usuario
            using var rCliente = await _http.SendAsync(Crear(HttpMethod.Post, "api/clientes", token, new
            {
                nombres = m.Nombres.Trim(),
                apellidos = m.Apellidos.Trim(),
                dni = m.Dni.Trim(),
                email,
                telefono = string.IsNullOrWhiteSpace(m.Telefono) ? null : m.Telefono.Trim(),
                user = new { id = userId }
            }));
            if (!rCliente.IsSuccessStatusCode)
                return new(false, true, "Tu cuenta se creó, pero no se pudo guardar tu ficha de cliente (¿DNI ya registrado?).");

            return new(true, true, "");
        }
        catch (HttpRequestException) { return new(false, false, "No se pudo conectar con el servidor. ¿Está encendido el backend?"); }
        catch (TaskCanceledException) { return new(false, false, "El servidor tardó demasiado en responder."); }
        catch (Exception ex) { return new(false, false, "Error inesperado: " + ex.Message); }
    }

    public async Task<(bool Ok, string Mensaje)> SolicitarRecuperacionAsync(string email)
    {
        try
        {
            var cuerpo = new StringContent(email.Trim(), Encoding.UTF8, "text/plain");
            var resp = await _http.PostAsync("api/account/reset-password/init", cuerpo);
            return resp.IsSuccessStatusCode
                ? (true, "")
                : (false, $"No se pudo procesar la solicitud ({(int)resp.StatusCode}).");
        }
        catch (HttpRequestException) { return (false, "No se pudo conectar con el servidor."); }
        catch (TaskCanceledException) { return (false, "El servidor tardó demasiado en responder."); }
    }

    public async Task<(bool Ok, string Mensaje)> RestablecerAsync(string key, string nuevaPassword)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/account/reset-password/finish",
                new { key, newPassword = nuevaPassword });
            if (resp.IsSuccessStatusCode) return (true, "");
            return (false, (int)resp.StatusCode == 400
                ? "La contraseña no es válida."
                : "El enlace no es válido o ya venció. Solicita uno nuevo.");
        }
        catch (HttpRequestException) { return (false, "No se pudo conectar con el servidor."); }
        catch (TaskCanceledException) { return (false, "El servidor tardó demasiado en responder."); }
    }

    private static HttpRequestMessage Crear(HttpMethod metodo, string url, string? token, object? cuerpo = null)
    {
        var req = new HttpRequestMessage(metodo, url);
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null)
            req.Content = JsonContent.Create(cuerpo);
        return req;
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        if (body.Contains("login-already-used") || body.Contains("email-already-used")
            || body.Contains("userexists") || body.Contains("emailexists"))
            return "Ya existe una cuenta con ese correo.";
        if (body.Contains("invalid-password"))
            return "La contraseña no cumple los requisitos.";
        return $"No se pudo completar el registro ({(int)resp.StatusCode}).";
    }
}
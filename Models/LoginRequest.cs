using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlazorApp1.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Ingresa tu correo o usuario")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contraseña")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("rememberMe")]
    public bool RememberMe { get; set; }
}

public class LoginResponse
{
    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
}

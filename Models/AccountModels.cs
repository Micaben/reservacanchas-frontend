using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Models;

public record RegistroResultado(bool Ok, bool Ingresado, string Mensaje);

public class RegistroModel
{
    [Required(ErrorMessage = "Ingresa tus nombres")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Nombres { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tus apellidos")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Apellidos { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu DNI")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string Dni { get; set; } = "";

    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Ingresa tu correo")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa una contraseña")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Repite la contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = "";
}

public class RecuperarModel
{
    [Required(ErrorMessage = "Ingresa tu correo")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    public string Email { get; set; } = "";
}

public class RestablecerModel
{
    [Required(ErrorMessage = "Ingresa la nueva contraseña")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Repite la contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = "";
}
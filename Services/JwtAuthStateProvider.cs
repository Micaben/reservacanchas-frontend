using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorApp1.Services;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));
    private readonly AuthService _auth;

    public JwtAuthStateProvider(AuthService auth)
    {
        _auth = auth;
        _auth.AuthChanged += () =>
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _auth.GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return Anonymous;

        var claims = ParseClaims(token);
        return claims is null
            ? Anonymous
            : new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }

    private static List<Claim>? ParseClaims(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return null;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = doc.RootElement;

            if (root.TryGetProperty("exp", out var exp) &&
                DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) < DateTimeOffset.UtcNow)
                return null; // token vencido

            var claims = new List<Claim>();
            if (root.TryGetProperty("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.Name, sub.GetString() ?? ""));
            if (root.TryGetProperty("auth", out var auth))
                foreach (var role in (auth.GetString() ?? "")
                         .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    claims.Add(new Claim(ClaimTypes.Role, role));
            return claims;
        }
        catch { return null; }
    }
}
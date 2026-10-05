namespace TaskFlow.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskFlow";
    public string Audience { get; set; } = "TaskFlow.Clients";

    /// <summary>Clave de firma HMAC. Mínimo 32 caracteres. Se inyecta por entorno (Jwt__Key), nunca en el repo.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;
}

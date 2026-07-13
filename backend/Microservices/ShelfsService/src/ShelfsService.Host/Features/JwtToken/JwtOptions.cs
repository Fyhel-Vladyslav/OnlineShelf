namespace ShelfsService.src.ShelfsService.Host.Features.JwtToken;
public sealed class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public double ExpiryMinutes { get; set; }
}
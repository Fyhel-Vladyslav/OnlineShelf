using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using FastEndpoints.Security;
using UserService.src.Features.JwtToken;

namespace UserService.src.Features.Authorization;
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(Guid userId, string role, TimeSpan timeSpan)
    {
        if (string.IsNullOrEmpty(_options.SigningKey))
        {
            throw new InvalidOperationException("JwtOptions:SigningKey is not configured.");
        }

        return JwtBearer.CreateToken(o =>
        {
            o.SigningKey = _options.SigningKey;
            o.Issuer = _options.Authority ;
            o.Audience = _options.Audience;
            o.ExpireAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

            o.User.Claims.Add(("id", userId.ToString()));
            o.User.Claims.Add((System.Security.Claims.ClaimTypes.Role, role));
        });
    }
}

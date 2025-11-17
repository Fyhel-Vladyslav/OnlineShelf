using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using FastEndpoints.Security;
using System.Security.Claims;
using UserService.src.UserService.Common;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Host.Features.JwtToken;
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(Guid userId, List<UserRoleLink> roles, TimeSpan timeSpan)
    {
        if (string.IsNullOrEmpty(_options.SigningKey))
        {
            throw new InvalidOperationException("JwtOptions:SigningKey is not configured.");
        }

        return JwtBearer.CreateToken(o =>
        {
            o.SigningKey = _options.SigningKey;
            o.Issuer = _options.Authority;
            o.Audience = _options.Audience;

            o.ExpireAt = DateTime.UtcNow.Add(timeSpan);

            o.User.Claims.Add((JwtRegisteredClaimNames.Sub, userId.ToString()));
            o.User.Claims.Add((JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            foreach (var role in roles)
            {
                o.User.Claims.Add((ClaimTypes.Role, role.Role.ToString()));
            }
        });
    }
}

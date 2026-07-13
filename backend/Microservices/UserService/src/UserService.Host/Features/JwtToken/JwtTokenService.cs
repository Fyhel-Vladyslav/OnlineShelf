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

            o.User.Claims.Add((JwtRegisteredClaimNames.NameId, userId.ToString()));
            o.User.Claims.Add((JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            if (roles != null)
            {
                foreach (var roleLink in roles)
                {
                    if (roleLink.Role?.Name != null)
                    {
                        o.User.Claims.Add(
                            (
                            ClaimTypes.Role,
                            roleLink.Role.Name
                            )
                        );
                    }
                }
            }
        });
    }
}

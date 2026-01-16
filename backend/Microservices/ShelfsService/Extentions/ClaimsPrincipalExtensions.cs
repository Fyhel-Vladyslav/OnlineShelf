using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace ShelfsService.Extentions;
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(id))
            throw new SecurityTokenException("UserId claim is missing");

        return Guid.Parse(id);
    }
}

using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace ShelfsService.Extentions;
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(id))
            return Guid.Empty;

        return Guid.Parse(id);
    }
}

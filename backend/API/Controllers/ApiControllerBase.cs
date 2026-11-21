using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirst(ClaimTypes.NameIdentifier);

            var userIdValue = userIdClaim?.Value;

            return Guid.TryParse(userIdValue, out userId);
        }

    }
}

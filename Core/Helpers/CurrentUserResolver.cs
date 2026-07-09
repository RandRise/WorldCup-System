using System.Security.Claims;
using Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Helpers
{
    public static class CurrentUserResolver
    {
        private const string JwtSubClaimType = "sub";
        private const string JwtEmailClaimType = "email";

        public static async Task<long> ResolveUserIdAsync(ClaimsPrincipal principal, UserManager<User> userManager)
        {
            long? userId = TryResolveUserId(principal);
            if (userId.HasValue)
            {
                return userId.Value;
            }

            string? email = TryResolveEmail(principal);
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Authenticated user identity claim is missing.");
            }

            User? user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                throw new KeyNotFoundException("Authenticated user was not found.");
            }

            return user.Id;
        }

        public static long? TryResolveUserId(ClaimsPrincipal principal)
        {
            string? subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtSubClaimType);
            if (long.TryParse(subject, out long userId))
            {
                return userId;
            }

            return null;
        }

        public static string? TryResolveEmail(ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue(JwtEmailClaimType);
        }
    }
}

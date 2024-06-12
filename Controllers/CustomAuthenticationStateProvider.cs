using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace Webstore.Controllers
{
    public class CustomAuthenticationStateProvider : ServerAuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var authState = await base.GetAuthenticationStateAsync();
            var user = authState.User;

            // Add custom roles to the ClaimsPrincipal
            user.AddIdentity(new ClaimsIdentity(new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Role, "Seller")
        }));

            return authState;
        }
    }

}

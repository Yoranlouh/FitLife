using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace WA.Auth;

public class AuthStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new ClaimsPrincipal(
            new ClaimsIdentity()
        );

        return Task.FromResult(new AuthenticationState(anonymous));
    }
}
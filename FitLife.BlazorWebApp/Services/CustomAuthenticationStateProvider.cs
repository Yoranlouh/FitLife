using System.Security.Claims;
using FitLife.BlazorWebApp.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitLife.BlazorWebApp.Services;

// Blazor's authentication system requires an AuthenticationStateProvider to know
// who the current user is. This implementation reads the ".FitLife.Auth" cookie,
// looks up the associated session, and builds a ClaimsPrincipal from it.
// The result is cached per Blazor circuit so it is not recalculated on every render.
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ISessionService       _sessionService;
    private readonly IHttpContextAccessor  _httpContextAccessor;

    // Cached authentication state for the lifetime of one Blazor circuit (SignalR connection).
    // Reset to null on logout so the next call re-reads the cookie.
    private AuthenticationState? _cachedState;

    public CustomAuthenticationStateProvider(ISessionService       sessionService,
                                             IHttpContextAccessor  httpContextAccessor)
    {
        _sessionService      = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    // Called by Blazor's [Authorize] attribute and AuthenticationState cascade.
    // Returns a ClaimsPrincipal built from the session, or an anonymous identity when not logged in.
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Return the cached state to avoid re-reading HttpContext during the WebSocket phase
        if (_cachedState != null)
            return Task.FromResult(_cachedState);

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _cachedState = Anonymous();
                return Task.FromResult(_cachedState);
            }

            // Resolve the session ID — prefer the explicit auth cookie over the generic session ID
            string sessionId;
            if (httpContext.Request.Cookies.TryGetValue(".FitLife.Auth", out var token))
            {
                sessionId = token;
            }
            else
            {
                if (string.IsNullOrEmpty(httpContext.Session.Id))
                    httpContext.Session.SetString("init", "true");
                sessionId = httpContext.Session.Id;
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                _cachedState = Anonymous();
                return Task.FromResult(_cachedState);
            }

            var userSession = _sessionService.GetUserSession(sessionId);
            if (userSession?.IsAuthenticated == true)
            {
                // Build ASP.NET Core Claims from the stored session data.
                // ClaimTypes.Role is what [Authorize(Roles = "...")] checks against.
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                    new Claim(ClaimTypes.Name,           userSession.DisplayName),
                    new Claim(ClaimTypes.Email,          userSession.Email),
                    new Claim(ClaimTypes.Role,           userSession.Role)
                };
                _cachedState = new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth")));
            }
            else
            {
                _cachedState = Anonymous();
            }
        }
        catch
        {
            _cachedState = Anonymous();
        }

        return Task.FromResult(_cachedState);
    }

    // Called after login or logout to clear the cache and force Blazor to re-evaluate
    // [Authorize] attributes throughout the component tree.
    public void NotifyAuthenticationStateChanged()
    {
        _cachedState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // Returns an unauthenticated ClaimsPrincipal (no claims, no identity name).
    // Used when no valid session is found.
    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}

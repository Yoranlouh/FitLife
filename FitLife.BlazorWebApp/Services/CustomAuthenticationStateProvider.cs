using System.Security.Claims;
using FitLife.BlazorWebApp.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Custom authentication state provider that uses in-memory session for session management
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ISessionService _sessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Cached per Blazor circuit (Scoped DI) — avoids re-reading HttpContext during WebSocket phase
    private AuthenticationState? _cachedState;

    public CustomAuthenticationStateProvider(ISessionService sessionService, IHttpContextAccessor httpContextAccessor)
    {
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
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
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                    new Claim(ClaimTypes.Name, userSession.DisplayName),
                    new Claim(ClaimTypes.Email, userSession.Email),
                    new Claim(ClaimTypes.Role, userSession.Role)
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

    public void NotifyAuthenticationStateChanged()
    {
        _cachedState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
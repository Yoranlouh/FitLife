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

    public CustomAuthenticationStateProvider(ISessionService sessionService, IHttpContextAccessor httpContextAccessor)
    {
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return string.Empty;

        // Try to get existing auth token from cookie
        if (httpContext.Request.Cookies.TryGetValue(".FitLife.Auth", out var existingToken))
        {
            return existingToken;
        }

        // Fallback to session ID
        if (string.IsNullOrEmpty(httpContext.Session.Id))
        {
            httpContext.Session.SetString("init", "true");
        }

        return httpContext.Session.Id;
    }

    /// <summary>
    /// Gets the current authentication state from in-memory session
    /// </summary>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            var userSession = _sessionService.GetUserSession(sessionId);

            if (userSession != null && userSession.IsAuthenticated)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                    new Claim(ClaimTypes.Name, userSession.DisplayName),
                    new Claim(ClaimTypes.Email, userSession.Email),
                    new Claim(ClaimTypes.Role, userSession.Role)
                };

                var identity = new ClaimsIdentity(claims, "CustomAuth");
                var user = new ClaimsPrincipal(identity);

                return Task.FromResult(new AuthenticationState(user));
            }
        }
        catch
        {
            // If there's an error reading from storage, return anonymous user
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    /// <summary>
    /// Notifies that the authentication state has changed
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
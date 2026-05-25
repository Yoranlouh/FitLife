using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Custom authentication handler for ASP.NET Core authentication middleware
/// Works in conjunction with CustomAuthenticationStateProvider for Blazor
/// </summary>
public class CustomAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ISessionService _sessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor)
        : base(options, logger, encoder)
    {
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Try to get auth token from cookie first
            string sessionId;
            if (httpContext.Request.Cookies.TryGetValue(".FitLife.Auth", out var existingToken))
            {
                sessionId = existingToken;
            }
            else
            {
                // Fallback to session ID
                if (string.IsNullOrEmpty(httpContext.Session.Id))
                {
                    httpContext.Session.SetString("init", "true");
                }
                sessionId = httpContext.Session.Id;
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Get user session from memory
            var userSession = _sessionService.GetUserSession(sessionId);
            if (userSession == null || !userSession.IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Create claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                new Claim(ClaimTypes.Name, userSession.DisplayName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during authentication");
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }
    }
}

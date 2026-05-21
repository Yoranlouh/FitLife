using System.Security.Claims;
using Blazored.LocalStorage;
using FitLife.BlazorWebApp.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Custom authentication state provider that uses local storage for session management
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private const string UserSessionKey = "fitlife_admin_session";

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Gets the current authentication state from local storage
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userSession = await _localStorage.GetItemAsync<UserSessionDto>(UserSessionKey);

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

                return new AuthenticationState(user);
            }
        }
        catch
        {
            // If there's an error reading from storage, return anonymous user
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Notifies that the authentication state has changed
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
using FitLife.BlazorWebApp.Models;
using System.Collections.Concurrent;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// In-memory session storage service for Blazor Server
/// Stores user sessions without requiring JavaScript interop
/// </summary>
public interface ISessionService
{
    void SetUserSession(string sessionId, UserSessionDto user);
    UserSessionDto? GetUserSession(string sessionId);
    void RemoveUserSession(string sessionId);
}

public class SessionService : ISessionService
{
    private readonly ConcurrentDictionary<string, UserSessionDto> _sessions = new();

    public void SetUserSession(string sessionId, UserSessionDto user)
    {
        _sessions[sessionId] = user;
    }

    public UserSessionDto? GetUserSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var user);
        return user;
    }

    public void RemoveUserSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}

using FitLife.BlazorWebApp.Models;
using System.Collections.Concurrent;

namespace FitLife.BlazorWebApp.Services;

// Contract for in-memory session management.
// Each logged-in admin/instructor gets a unique session ID (stored in a cookie)
// that maps to their UserSessionDto in a server-side dictionary.
public interface ISessionService
{
    // Stores or replaces a user session under the given session ID key
    void SetUserSession(string sessionId, UserSessionDto user);

    // Retrieves a user session by session ID; returns null if not found or expired
    UserSessionDto? GetUserSession(string sessionId);

    // Removes a session, used during logout
    void RemoveUserSession(string sessionId);
}

// Thread-safe implementation using ConcurrentDictionary.
// Registered as Singleton so all Blazor circuit scopes share the same dictionary.
// Sessions are held in memory — they are lost if the server restarts.
public class SessionService : ISessionService
{
    // ConcurrentDictionary is safe for concurrent reads and writes
    // without explicit locking — important for Blazor Server's multi-threaded model.
    private readonly ConcurrentDictionary<string, UserSessionDto> _sessions = new();

    // Inserts or overwrites the session entry for the given session ID
    public void SetUserSession(string sessionId, UserSessionDto user)
    {
        _sessions[sessionId] = user;
    }

    // Returns the user associated with the session ID, or null when not found
    public UserSessionDto? GetUserSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var user);
        return user;
    }

    // Removes the session entry (called on logout or cookie deletion)
    public void RemoveUserSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}

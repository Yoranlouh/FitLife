using System.Collections.Concurrent;

namespace FitLife.BlazorWebApp.Services;

public enum PaymentStatus { Pending, Confirmed, Cancelled }

public class PaymentSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public string FirstName    { get; set; } = "";
    public string LastName     { get; set; } = "";
    public string Email        { get; set; } = "";
    public string Plan         { get; set; } = "";
    public bool   IsYearly     { get; set; }
    public decimal Amount      { get; set; }
    public string SubMethod    { get; set; } = "ideal";
    public string? Bank        { get; set; }
    public string? Phone       { get; set; }
    public string PasswordHash { get; set; } = "";
    public DateTime StartDate  { get; set; } = DateTime.Today;
    public PaymentStatus Status    { get; set; } = PaymentStatus.Pending;
    public bool          IsRegistered { get; set; } = false;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}

public interface IPaymentService
{
    Task<PaymentSession> CreateSessionAsync(
        string firstName, string lastName, string email,
        string plan, bool isYearly, decimal amount,
        string subMethod, string? bank, string? phone,
        string passwordHash, DateTime startDate);

    PaymentSession? GetSession(string sessionId);
    bool ConfirmSession(string sessionId);
    bool CancelSession(string sessionId);
}

public class PaymentService : IPaymentService
{
    private readonly ConcurrentDictionary<string, PaymentSession> _sessions = new();

    public Task<PaymentSession> CreateSessionAsync(
        string firstName, string lastName, string email,
        string plan, bool isYearly, decimal amount,
        string subMethod, string? bank, string? phone,
        string passwordHash, DateTime startDate)
    {
        var session = new PaymentSession
        {
            FirstName    = firstName,
            LastName     = lastName,
            Email        = email,
            Plan         = plan,
            IsYearly     = isYearly,
            Amount       = amount,
            SubMethod    = subMethod,
            Bank         = bank,
            Phone        = phone,
            PasswordHash = passwordHash,
            StartDate    = startDate,
        };
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public PaymentSession? GetSession(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public bool ConfirmSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var s) && s.Status == PaymentStatus.Pending)
        {
            s.Status = PaymentStatus.Confirmed;
            return true;
        }
        return false;
    }

    public bool CancelSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var s))
        {
            s.Status = PaymentStatus.Cancelled;
            return true;
        }
        return false;
    }
}
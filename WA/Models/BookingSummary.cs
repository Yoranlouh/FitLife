namespace WA.Models;

public class BookingSummary
{
    public string MovieTitle { get; init; } = string.Empty;
    public DateTimeOffset StartsAt { get; init; }
    public IReadOnlyList<TicketSelection> Lines { get; init; } = [];
    public decimal TotalPrice { get; init; }
}


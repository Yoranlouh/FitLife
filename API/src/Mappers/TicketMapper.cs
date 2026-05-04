using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers;

public class TicketMapper
{
    public static TicketResponse ToResponse(Ticket ticket)
    {
        var showDateTimeParse = DateTimeOffset.ParseExact(ticket.ShowDateTimeUtc,
            "O", // Exact ISO 8601 round-trip format
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None);
        return new TicketResponse
        {
            Id = ticket.Id,
            ShowingId = ticket.ShowingId,
            MovieTitle = ticket.Showing?.Movie?.Title ?? "",
            ShowDateTimeUtc = showDateTimeParse,
            SeatNumber = ticket.SeatNumber,
            TicketType = ticket.TicketType,
            Status = ticket.Status,
            PaymentStatus = ticket.PaymentStatus,
            QrCodeGuid = ticket.QrCodeGuid,
            QrIsActive = ticket.QrIsActive,
            Price = ticket.Price,
            PurchaseDateUtc = ticket.PurchaseDate
        };
    }

    public static IEnumerable<TicketResponse> ToResponse(IEnumerable<Ticket> tickets)
    {
        return tickets.Select(ToResponse);
    }

    public static Ticket ToEntity(TicketRequest request)
    {
        return new Ticket
        {
            ShowingId = request.ShowingId,
            ShowDateTimeUtc = request.ShowDateTimeUtc.ToString("O"),
            SeatNumber = request.SeatNumber,
            TicketType = request.TicketType,
            Price = request.Price,
            PurchaseDate = DateTimeOffset.UtcNow
        };
    }
}
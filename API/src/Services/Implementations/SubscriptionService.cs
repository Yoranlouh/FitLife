using API.Infrastructure.Database;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApiDbContext _context;

        public SubscriptionService(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionStatusResponse> GetStatusAsync(int memberId)
        {
            var member = await _context.Members
                .Include(m => m.Subscription)
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null) throw new KeyNotFoundException("Member not found");

            return MapToStatusResponse(member);
        }

        public async Task<SubscriptionStatusResponse> UpgradeAsync(int memberId, SubscriptionUpgradeRequest request)
        {
            var member = await _context.Members
                .Include(m => m.Subscription)
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null) throw new KeyNotFoundException("Member not found");

            var newSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == request.NewSubscriptionId);

            if (newSubscription == null) throw new KeyNotFoundException("Subscription not found");

            // Upgrade-logica: 
            // Als er al een abonnement is, kijken we of het een upgrade is (hogere prijs of langere duur)
            // In dit geval passen we het gewoon direct aan voor de demo/oefening.
            member.SubscriptionId = newSubscription.Id;
            member.Subscription = newSubscription;
            
            // Verlenging van de huidige einddatum met de duur van het nieuwe abonnement 
            // of start vandaag als er geen actief abonnement was.
            var startDate = (member.SubscriptionEndDate > DateTime.UtcNow) 
                ? member.SubscriptionEndDate.Value 
                : DateTime.UtcNow;
            
            member.SubscriptionStartDate = DateTime.UtcNow; // Moment van upgrade
            member.SubscriptionEndDate = startDate.AddMonths(newSubscription.DurationInMonths);
            member.Status = "Active";

            await _context.SaveChangesAsync();
            return MapToStatusResponse(member);
        }

        public async Task<SubscriptionStatusResponse> RenewAsync(int memberId, SubscriptionRenewRequest request)
        {
            var member = await _context.Members
                .Include(m => m.Subscription)
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null) throw new KeyNotFoundException("Member not found");
            if (member.Subscription == null) throw new InvalidOperationException("No subscription to renew");

            var monthsToAdd = request.DurationInMonths ?? member.Subscription.DurationInMonths;
            
            var startDate = (member.SubscriptionEndDate > DateTime.UtcNow) 
                ? member.SubscriptionEndDate.Value 
                : DateTime.UtcNow;

            member.SubscriptionEndDate = startDate.AddMonths(monthsToAdd);
            member.Status = "Active";

            await _context.SaveChangesAsync();
            return MapToStatusResponse(member);
        }

        public async Task<PriceCalculationResponse> CalculatePriceAsync(int subscriptionId, int durationInMonths)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null) throw new KeyNotFoundException("Subscription not found");

            decimal basePrice = subscription.Price;
            decimal totalPrice = basePrice * durationInMonths;
            
            // Regels voor maand/jaar abonnementen
            // Bijv. 10% korting bij 12 maanden
            string description = $"{durationInMonths} maanden {subscription.Name}";
            if (durationInMonths >= 12)
            {
                totalPrice *= 0.9m;
                description += " (10% jaarkorting toegepast)";
            }

            return new PriceCalculationResponse
            {
                TotalPrice = totalPrice,
                MonthlyPrice = totalPrice / durationInMonths,
                Description = description
            };
        }

        public async Task ProcessNotificationsAsync()
        {
            // 6-weken-notificatie logica
            var targetDate = DateTime.UtcNow.AddDays(42); // 6 weken
            
            var membersToNotify = await _context.Members
                .Where(m => m.SubscriptionEndDate.HasValue && 
                            m.SubscriptionEndDate.Value.Date == targetDate.Date)
                .ToListAsync();

            foreach (var member in membersToNotify)
            {
                // Hier zou je een mail-service aanroepen
                Console.WriteLine($"[NOTIFICATIE] Lid {member.FirstName} {member.LastName} ({member.Email}): " +
                                  $"Uw abonnement verloopt over 6 weken op {member.SubscriptionEndDate:dd-MM-yyyy}.");
            }
        }

        private SubscriptionStatusResponse MapToStatusResponse(Member member)
        {
            return new SubscriptionStatusResponse
            {
                MemberId = member.Id,
                SubscriptionName = member.Subscription?.Name,
                StartDate = member.SubscriptionStartDate,
                EndDate = member.SubscriptionEndDate,
                IsActive = member.SubscriptionEndDate > DateTime.UtcNow && member.Status == "Active",
                WillAutoRenew = member.IsAutoRenewEnabled,
                CurrentMonthlyPrice = member.Subscription?.Price ?? 0
            };
        }
    }
}

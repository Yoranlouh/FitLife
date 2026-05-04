using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace API.Infrastructure.Database
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApiDbContext db)
        {
            if (!await db.Users.AnyAsync())
            {
                db.Users.AddRange(
                    new User("Admin"),
                    new User("TestUser"),
                    new User("John Doe"),
                    new User("Jane Smith")
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
            {
                var auditoriumsRequest = new List<CreateAuditoriumRequest>
                {
                    new CreateAuditoriumRequest("Zaal 1", new List<RowConfig>
                    {
                        new RowConfig(15, 2),
                        new RowConfig(15, 0),
                        new RowConfig(15, 2),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 4)
                    }),
                    new CreateAuditoriumRequest("Zaal 2", new List<RowConfig>
                    {
                        new RowConfig(15, 2),
                        new RowConfig(15, 0),
                        new RowConfig(15, 2),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 4)
                    }),
                    new CreateAuditoriumRequest("Zaal 3", new List<RowConfig>
                    {
                        new RowConfig(15, 2),
                        new RowConfig(15, 0),
                        new RowConfig(15, 2),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(15, 4)
                    }),
                    new CreateAuditoriumRequest("Zaal 4", new List<RowConfig>
                    {
                        new RowConfig(10, 0),
                        new RowConfig(10, 1),
                        new RowConfig(10, 2),
                        new RowConfig(10, 0),
                        new RowConfig(10, 1),
                        new RowConfig(10, 2)
                    }),
                    new CreateAuditoriumRequest("Zaal 5", new List<RowConfig>
                    {
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(10, 0),
                        new RowConfig(10, 0)
                    }),
                    new CreateAuditoriumRequest("Zaal 6", new List<RowConfig>
                    {
                        new RowConfig(15, 0),
                        new RowConfig(15, 0),
                        new RowConfig(10, 0),
                        new RowConfig(10, 0)
                    }),
                };
                foreach (var request in auditoriumsRequest)
                {
                    await auditoriumService.AddAuditoriumAsync(request);
                }
            }

            // SEED SHOWINGS
            var movies = await db.Movies.ToListAsync();
            var auditoriums = await db.Auditoriums.ToListAsync();

            var random = new Random();
            var showings = new List<Showing>();

            var dutchMovies = movies
                .Where(m => m.SpokenLanguageCodeIso6391 == "nl")
                .ToList();

            var kidsMovies = movies
                .Where(m => int.TryParse(m.AgeIndication, out var age) && age < 12)
                .ToList();

// tijdslots tussen 10:00 en 23:00 (ongeveer elke 2 uur)
            var baseDate = DateTimeOffset.UtcNow.Date;
            var timeSlots = new List<DateTimeOffset>();

            for (int hour = 10; hour <= 23; hour += 2)
            {
                timeSlots.Add(baseDate.AddHours(hour));
            }

// shuffle + basis selectie
            var selectedMovies = movies
                .OrderBy(_ => random.Next())
                .Take(10)
                .ToList();

// forceer NL film
            if (dutchMovies.Any())
            {
                var dutchMovie = dutchMovies[random.Next(dutchMovies.Count)];
                selectedMovies = selectedMovies.Where(m => m.Id != dutchMovie.Id).ToList();
                selectedMovies.Add(dutchMovie);
            }

// forceer kids film (<12)
            if (kidsMovies.Any())
            {
                var kidsMovie = kidsMovies[random.Next(kidsMovies.Count)];
                selectedMovies = selectedMovies.Where(m => m.Id != kidsMovie.Id).ToList();
                selectedMovies.Add(kidsMovie);
            }

// max 12 totaal
            selectedMovies = selectedMovies.Take(12).ToList();

// maak showings
            for (int i = 0; i < selectedMovies.Count; i++)
            {
                var movie = selectedMovies[i];
                var auditorium = auditoriums[i % auditoriums.Count];
                var time = timeSlots[i % timeSlots.Count];

                showings.Add(new Showing
                {
                    MovieId = movie.Id,
                    AuditoriumId = auditorium.Id,
                    StartsAt = time,
                    IsThreeD = random.Next(0, 2) == 0,
                    AuditoriumLayoutSnapshot = auditorium.RowConfigJson
                });
            }

// reset + opslaan
            db.Showings.RemoveRange(db.Showings);
            db.Showings.AddRange(showings);
            await db.SaveChangesAsync();

            // Dummy order for API testing when no orders exist
            if (!await db.Orders.AnyAsync())
            {
                var showing = await db.Showings.OrderBy(s => s.Id).FirstOrDefaultAsync();
                if (showing != null)
                {
                    var ticket = new Ticket
                    {
                        ShowingId = showing.Id,
                        ShowDateTimeUtc = showing.StartsAt.UtcDateTime.ToString("O"),
                        SeatNumber = "A1",
                        Price = 9.50m,
                        TicketType = "Adult",
                        PaymentStatus = "Pending",
                        QrIsActive = false
                    };
                    await db.Tickets.AddAsync(ticket);
                    await db.SaveChangesAsync();

                    var order = new Order
                    {
                        OrderCode = "DUMMYORDER001",
                        CreatedAtUtc = DateTime.UtcNow,
                        TotalAmount = ticket.Price,
                        OrderType = "Reservation",
                        PaymentStatus = "Pending",
                        PaymentMethod = "IDEAL",
                        IsPrinted = false,
                        OrderTickets = new List<OrderTicket>
                        {
                            new OrderTicket { TicketId = ticket.Id, Ticket = ticket }
                        }
                    };

                    await db.Orders.AddAsync(order);
                    await db.SaveChangesAsync();
                }
            }

            if (!await db.Tickets.AnyAsync())
            {
                await ticketService.CreateTicketAsync(new Ticket
                {
                    ShowingId = 1,
                    ShowDateTimeUtc = DateTimeOffset.UtcNow.Date.AddHours(18).ToString("O"),
                    SeatNumber = "A1",
                    TicketType = "Adult",
                    Price = 8.50m
                });
            }
            
            if (!await db.Arrangements.AnyAsync())
            {
                var arr1 = new Arrangement
                {
                    Name = "Movie Deal - Popcorn & Cola",
                    Price = 12.00m,
                    IsActive = true
                };

                var arr2 = new Arrangement
                {
                    Name = "Movie Deal - M&M's & Fanta",
                    Price = 12.00m,
                    IsActive = true
                };

                db.Arrangements.AddRange(arr1, arr2);
                await db.SaveChangesAsync();
            }

            if (!await db.ArrangementItems.AnyAsync())
            {
                var arr1 = await db.Arrangements.FirstAsync(a => a.Name.Contains("Popcorn"));
                var arr2 = await db.Arrangements.FirstAsync(a => a.Name.Contains("M&M"));

                db.ArrangementItems.AddRange(
                    new ArrangementItem
                    {
                        ArrangementId = arr1.Id,
                        Type = ArrangementItemType.Ticket,
                        Name = "Ticket",
                        Quantity = 1
                    },
                    new ArrangementItem
                    {
                        ArrangementId = arr1.Id,
                        Type = ArrangementItemType.Food,
                        Name = "Popcorn",
                        Quantity = 1
                    },
                    new ArrangementItem
                    {
                        ArrangementId = arr1.Id,
                        Type = ArrangementItemType.Drink,
                        Name = "Cola",
                        Quantity = 1
                    },

                    new ArrangementItem
                    {
                        ArrangementId = arr2.Id,
                        Type = ArrangementItemType.Ticket,
                        Name = "Ticket",
                        Quantity = 1
                    },
                    new ArrangementItem
                    {
                        ArrangementId = arr2.Id,
                        Type = ArrangementItemType.Food,
                        Name = "M&M's",
                        Quantity = 1
                    },
                    new ArrangementItem
                    {
                        ArrangementId = arr2.Id,
                        Type = ArrangementItemType.Drink,
                        Name = "Fanta",
                        Quantity = 1
                    }
                );

                await db.SaveChangesAsync();
            }
            

            if (!await db.EmailSubscriptions.AnyAsync())
            {
                await localMailService.AddAsync("TheBeeKeerIsAmazing@Badazz.yow");
                await localMailService.AddAsync("Batman@adjlaskjd.nl");
                var textPart = new TextPart("plain")
                {
                    Text = @" Hello subscribers!,
                    
This is a test email to confirm that the subscription system is working correctly. Thank you for subscribing to our newsletter!

Groetjessssss,

CineNet."
                };
                await localMailService.SendEmailToSubscribersAsync(textPart, "CineNet", "Kom nu kijken!!");
            }

            await db.SaveChangesAsync();


            


        }
    }
    
    
    
}
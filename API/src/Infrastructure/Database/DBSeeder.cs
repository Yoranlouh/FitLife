using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;
using API.Domain.Model;

namespace API.Infrastructure.Database
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApiDbContext db)
        {
            // Seed Subscriptions
            if (!await db.Subscriptions.AnyAsync())
            {
                var subscriptions = new List<Subscription>
                {
                    new Subscription { Name = "Basic", Price = 29.99m, DurationInMonths = 1 },
                    new Subscription { Name = "Pro", Price = 49.99m, DurationInMonths = 1 },
                    new Subscription { Name = "Annual Elite", Price = 499.99m, DurationInMonths = 12 }
                };
                db.Subscriptions.AddRange(subscriptions);
                await db.SaveChangesAsync();
            }

            // Seed Instructors
            if (!await db.Instructors.AnyAsync())
            {
                var instructors = new List<Instructor>
                {
                    new Instructor { FirstName = "Marco", LastName = "Borsato", Email = "marco@fitlife.com", Specialization = "Spinning" },
                    new Instructor { FirstName = "Anouk", LastName = "Teeuwe", Email = "anouk@fitlife.com", Specialization = "Yoga" },
                    new Instructor { FirstName = "Ali", LastName = "B", Email = "ali@fitlife.com", Specialization = "HIIT" },
                    new Instructor { FirstName = "NNB", LastName = "", Email = "nnb@fitlife.com", Specialization = "Unknown" }
                };
                db.Instructors.AddRange(instructors);
                await db.SaveChangesAsync();
            }

            // Seed Locations
            if (!await db.Locations.AnyAsync())
            {
                var locations = new List<Location>
                {
                    new Location { Name = "Studio A", Address = "Hoofdstraat 1, Amsterdam", Capacity = 20 },
                    new Location { Name = "Studio B", Address = "Hoofdstraat 2, Amsterdam", Capacity = 15 },
                    new Location { Name = "Spinning Zone", Address = "Hoofdstraat 3, Amsterdam", Capacity = 25 }
                };
                db.Locations.AddRange(locations);
                await db.SaveChangesAsync();
            }

            // Seed Workouts
            if (!await db.Workouts.AnyAsync())
            {
                var workouts = new List<Workout>
                {
                    new Workout { Name = "Extreme Spinning", Description = "High intensity spinning session", Duration = TimeSpan.FromMinutes(45) },
                    new Workout { Name = "Zen Yoga", Description = "Relaxing yoga for all levels", Duration = TimeSpan.FromMinutes(60) },
                    new Workout { Name = "Power HIIT", Description = "Full body interval training", Duration = TimeSpan.FromMinutes(30) }
                };
                db.Workouts.AddRange(workouts);
                await db.SaveChangesAsync();
            }

            // Seed Lessons
            if (!await db.Lessons.AnyAsync())
            {
                var workout = await db.Workouts.FirstAsync();
                var instructor = await db.Instructors.FirstAsync();
                var location = await db.Locations.FirstAsync();

                var lessons = new List<Lesson>
                {
                    new Lesson 
                    { 
                        StartTime = DateTime.Now.AddDays(1).Date.AddHours(10), 
                        EndTime = DateTime.Now.AddDays(1).Date.AddHours(11),
                        MaxParticipants = 20,
                        WorkoutId = workout.Id,
                        InstructorId = instructor.Id,
                        LocationId = location.Id
                    },
                    new Lesson 
                    { 
                        StartTime = DateTime.Now.AddDays(1).Date.AddHours(14), 
                        EndTime = DateTime.Now.AddDays(1).Date.AddHours(15),
                        MaxParticipants = 20,
                        WorkoutId = workout.Id,
                        InstructorId = instructor.Id,
                        LocationId = location.Id
                    }
                };
                db.Lessons.AddRange(lessons);
                await db.SaveChangesAsync();

                // Seed SpinningBikes for the first lesson if it's spinning
                foreach (var lesson in lessons)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        db.SpinningBikes.Add(new SpinningBike { BikeNumber = i, LessonId = lesson.Id });
                    }
                }
                await db.SaveChangesAsync();
            }

            // Seed Members
            if (!await db.Members.AnyAsync())
            {
                var subscription = await db.Subscriptions.FirstAsync();
                var members = new List<Member>
                {
                    new Member { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", JoinDate = DateTime.Now.AddMonths(-3), SubscriptionId = subscription.Id },
                    new Member { FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", JoinDate = DateTime.Now.AddMonths(-1), SubscriptionId = subscription.Id }
                };
                db.Members.AddRange(members);
                await db.SaveChangesAsync();
            }

            // Seed Users
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

            // Seed EmailSubscriptions
            if (!await db.EmailSubscriptions.AnyAsync())
            {
                db.EmailSubscriptions.AddRange(
                    new EmailSubscription { Email = "newsletter@fitlife.com" },
                    new EmailSubscription { Email = "promo@fitlife.com" }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}

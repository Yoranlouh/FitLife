using Microsoft.EntityFrameworkCore;
using SharedLibrary.Domain.Entities;

namespace FitLife.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<Instructor> Instructors { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; }
    public DbSet<SpinningBike> SpinningBikes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>().ToTable("users");

        // Reservation configuratie
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            
            entity.HasOne(r => r.Lesson)
                .WithMany(l => l.Reservations)
                .HasForeignKey(r => r.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Member verwijst naar users tabel
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.Ignore(r => r.Member);
        });

        // Lesson configuratie
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
            
            entity.HasOne(l => l.Workout)
                .WithMany()
                .HasForeignKey(l => l.WorkoutId);

            entity.HasOne(l => l.Instructor)
                .WithMany(i => i.Lessons)
                .HasForeignKey(l => l.InstructorId);

            entity.HasOne(l => l.Location)
                .WithMany()
                .HasForeignKey(l => l.LocationId);
        });

        // WaitlistEntry configuratie
        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.ToTable("waitlist_entries");
            entity.Ignore(w => w.Member);
        });

        // Overige tabellen
        modelBuilder.Entity<Workout>().ToTable("workouts");
        modelBuilder.Entity<Instructor>().ToTable("instructors");
        modelBuilder.Entity<Location>().ToTable("locations");
        modelBuilder.Entity<SpinningBike>().ToTable("spinning_bikes");
    }
}

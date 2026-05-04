using API.Domain.Model;

namespace API.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using SharedLibrary.Domain.Entities;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
    {
    }


    public DbSet<User> Users => Set<User>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<SpinningBike> SpinningBikes => Set<SpinningBike>();
    public DbSet<Photo> Photos => Set<Photo>();

    // Email related entities
    public DbSet<EmailSubscription> EmailSubscriptions => Set<EmailSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User & Photo
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Photo)
                  .WithMany()
                  .HasForeignKey(e => e.PhotoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("photos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.StorageKey).IsRequired();
        });

        // Workout
        modelBuilder.Entity<Workout>(entity =>
        {
            entity.ToTable("workouts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Instructor
        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.ToTable("instructors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Photo)
                  .WithMany()
                  .HasForeignKey(e => e.PhotoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Location
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Lesson
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Workout)
                  .WithMany(w => w.Lessons)
                  .HasForeignKey(e => e.WorkoutId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Instructor)
                  .WithMany(i => i.Lessons)
                  .HasForeignKey(e => e.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Location)
                  .WithMany(l => l.Lessons)
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Subscription
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // Member
        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("members");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(e => e.Photo)
                  .WithMany()
                  .HasForeignKey(e => e.PhotoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Subscription)
                  .WithMany(s => s.Members)
                  .HasForeignKey(e => e.SubscriptionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Reservation
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Member)
                  .WithMany(m => m.Reservations)
                  .HasForeignKey(e => e.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lesson)
                  .WithMany(l => l.Reservations)
                  .HasForeignKey(e => e.LessonId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SpinningBike)
                  .WithOne(b => b.Reservation)
                  .HasForeignKey<Reservation>(e => e.SpinningBikeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // WaitlistEntry
        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.ToTable("waitlist_entries");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Member)
                  .WithMany(m => m.WaitlistEntries)
                  .HasForeignKey(e => e.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lesson)
                  .WithMany(l => l.WaitlistEntries)
                  .HasForeignKey(e => e.LessonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SpinningBike
        modelBuilder.Entity<SpinningBike>(entity =>
        {
            entity.ToTable("spinning_bikes");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Lesson)
                  .WithMany(l => l.SpinningBikes)
                  .HasForeignKey(e => e.LessonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Email related entities
        modelBuilder.Entity<EmailSubscription>(entity =>
        {
            entity.ToTable("email_subscriptions");
            entity.HasKey(e => e.Id);
        });
    }
}
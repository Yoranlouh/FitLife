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

    // Email related entities
    public DbSet<EmailSubscription> EmailSubscriptions => Set<EmailSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("users");

        // Email related entities
        modelBuilder.Entity<EmailSubscription>().ToTable("email_subscriptions");
    }
}
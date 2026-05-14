using FitLife.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitLife.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Eventuele configuratie voor User entiteit
        modelBuilder.Entity<User>().HasKey(u => u.Id);
    }
}

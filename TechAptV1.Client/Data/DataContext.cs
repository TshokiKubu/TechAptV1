using Microsoft.EntityFrameworkCore;
using TechAptV1.Client.Models;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    public DbSet<Number> Numbers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the Number entity
        modelBuilder.Entity<Number>(entity =>
        {
            entity.HasKey(e => e.Id); // Primary key
            entity.Property(e => e.Value)
                  .IsRequired()       // Value is mandatory
                  .HasColumnType("INTEGER"); // Explicit column type
            entity.Property(e => e.IsPrime)
                  .IsRequired();      // IsPrime is mandatory
        });

        base.OnModelCreating(modelBuilder); // Ensure base configurations are applied
    }
}


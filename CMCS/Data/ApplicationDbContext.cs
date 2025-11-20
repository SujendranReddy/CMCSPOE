using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CMCS;
using CMCS.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ensure decimal precision for ApplicationUser HourlyRate
        builder.Entity<ApplicationUser>()
               .Property(u => u.HourlyRate)
               .HasPrecision(18, 2);

        // Ensure decimal precision for Claim HourlyRate
        builder.Entity<Claim>()
               .Property(u => u.HourlyRate)
               .HasPrecision(18, 2);
    }

    // Claims table in the database
    public DbSet<Claim> Claims { get; set; }
}

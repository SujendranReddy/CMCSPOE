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

        builder.Entity<ApplicationUser>()
               .Property(u => u.HourlyRate)
               .HasPrecision(18, 2);

        builder.Entity<Claim>()
               .Property(u => u.HourlyRate)
               .HasPrecision(18, 2);
    }
    public DbSet<Claim> Claims { get; set; }

}

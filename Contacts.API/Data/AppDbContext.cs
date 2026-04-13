using Contacts.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Contacts.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>()
            .Ignore(c => c.Age);

        modelBuilder.Entity<Contact>()
            .Property(c => c.Gender)
            .HasConversion<string>();
    }
}

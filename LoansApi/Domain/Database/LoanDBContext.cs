using LoansApi.Domain.Entities;

namespace LoansApi.Domain.Database;

using Microsoft.EntityFrameworkCore;

public class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Loan> Loans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Loan>()
            .HasKey(l => l.Id);
        
        modelBuilder.Entity<Loan>()
            .Property(l => l.UserId)
            .IsRequired();

        modelBuilder.Entity<Loan>()
            .HasOne<User>()               
            .WithMany(u => u.Loans)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();
    }
}

using BankingTransfers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingTransfers.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserProfileAccountPermissions> UserProfileAccountPermissions => Set<UserProfileAccountPermissions>();
    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

using Microsoft.EntityFrameworkCore;
using StockApp.Api.Models;

namespace StockApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RiskProfile> RiskProfiles => Set<RiskProfile>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<TradeSignal> TradeSignals => Set<TradeSignal>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne(u => u.RiskProfile)
            .WithOne(r => r.User)
            .HasForeignKey<RiskProfile>(r => r.UserId);

        modelBuilder.Entity<Stock>()
            .HasIndex(s => s.Symbol)
            .IsUnique();
    }
}

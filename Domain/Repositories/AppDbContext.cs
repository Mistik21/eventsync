using Microsoft.EntityFrameworkCore;
using eventsynс.Domain.Entities;

namespace eventsynс.Domain.Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Cost> Costs => Set<Cost>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
using Microsoft.EntityFrameworkCore;
using OrderContext.Infratructure.Configurations;

namespace OrderContext.Infratructure;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
    }
}

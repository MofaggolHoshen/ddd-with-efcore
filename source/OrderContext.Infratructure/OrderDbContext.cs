using Microsoft.EntityFrameworkCore;
using OrderContext.Domain.Common;
using OrderContext.Infratructure.Configurations;

namespace OrderContext.Infratructure;

public class OrderDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _dispatcher;

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public OrderDbContext(DbContextOptions<OrderDbContext> options, IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public DbSet<Client> Clients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect all pending domain events from tracked entities before saving
        List<IAggregateRoot> entities = ChangeTracker.Entries()
                                                    .Select(e => e.Entity)
                                                    .OfType<IAggregateRoot>()
                                                    .Where(e => e.DomainEvents.Count > 0)
                                                    .ToList();

        List<IDomainEvent> domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events on entities before save so re-entrant saves don't re-dispatch
        entities.ForEach(e => e.ClearDomainEvents());

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch events after successful save
        if (_dispatcher is not null)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _dispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }

        return result;
    }
}

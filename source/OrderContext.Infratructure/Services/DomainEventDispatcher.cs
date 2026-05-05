using Microsoft.Extensions.DependencyInjection;
using OrderContext.Domain.Common;

namespace OrderContext.Infratructure.Services;

/// <summary>
/// Resolves and invokes all registered <see cref="IDomainEventHandler{TEvent}"/> instances
/// for a given domain event using the DI container.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            // Cast to dynamic so we can call the generic HandleAsync without reflection
            await ((dynamic)handler!).HandleAsync((dynamic)domainEvent, cancellationToken);
        }
    }
}

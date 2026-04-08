using OrderContext.Domain;
using OrderContext.Domain.Services;

namespace OrderContext.Infratructure.Services;

/// <summary>
/// Infrastructure implementation of IEmailUniquenessChecker.
/// Uses EF Core to query the database.
/// 
/// Lives in INFRASTRUCTURE LAYER because it depends on DbContext.
/// </summary>
public class EmailUniquenessChecker : IEmailUniquenessChecker
{
    private readonly OrderDbContext _context;

    public EmailUniquenessChecker(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public bool IsEmailUnique(Email email)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return !_context.Clients.Any(c => c.Email.Value == email.Value);
    }

    /// <inheritdoc />
    public bool IsEmailUnique(Email email, Guid excludeClientId)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return !_context.Clients
            .Where(c => c.Id != excludeClientId)
            .Any(c => c.Email.Value == email.Value);
    }
}

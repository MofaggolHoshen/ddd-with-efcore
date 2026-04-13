namespace OrderContext.Application.DTOs;

/// <summary>
/// Read-only DTO for client queries.
/// </summary>
/// <param name="Id">The unique identifier of the client.</param>
/// <param name="Name">The name of the client.</param>
/// <param name="Email">The email address of the client.</param>
/// <param name="CreatedAt">The date and time when the client was created.</param>
public record ClientDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt
);

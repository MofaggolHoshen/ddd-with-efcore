namespace OrderContext.Api.Contracts;

public sealed record CreateClientRequest(string Name, string Email);

public sealed record UpdateClientRequest(string Name, string Email);

using Microsoft.EntityFrameworkCore;
using OrderContext.Api.Contracts;
using OrderContext.Application;
using OrderContext.Application.Services;
using OrderContext.Infratructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? "Data Source=ordercontext.db";

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

var clients = app.MapGroup("/api/clients");

clients.MapGet("/", async (ClientApplicationService service, CancellationToken ct) =>
{
    var items = await service.GetAllClientsAsync(ct);
    return Results.Ok(items);
});

clients.MapGet("/{id:guid}", async (Guid id, ClientApplicationService service, CancellationToken ct) =>
{
    var client = await service.GetClientByIdAsync(id, ct);
    return client is null ? Results.NotFound() : Results.Ok(client);
});

clients.MapPost("/", async (CreateClientRequest request, ClientApplicationService service, CancellationToken ct) =>
{
    try
    {
        var id = await service.RegisterClientAsync(request.Name, request.Email, ct);
        return Results.Created($"/api/clients/{id}", new { Id = id });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { Error = ex.Message });
    }
});

clients.MapPut("/{id:guid}", async (Guid id, UpdateClientRequest request, ClientApplicationService service, CancellationToken ct) =>
{
    try
    {
        await service.UpdateClientNameAsync(id, request.Name, ct);
        await service.UpdateClientEmailAsync(id, request.Email, ct);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? Results.NotFound(new { Error = ex.Message })
            : Results.Conflict(new { Error = ex.Message });
    }
});

clients.MapDelete("/{id:guid}", async (Guid id, ClientApplicationService service, CancellationToken ct) =>
{
    try
    {
        await service.DeleteClientAsync(id, ct);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { Error = ex.Message });
    }
});

app.Run();

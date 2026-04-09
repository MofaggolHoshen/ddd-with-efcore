using OrderContext.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OrderContext;

public class Client
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core and internal use. Use the static Create method to instantiate a new Client.
    /// </summary>
    private Client()
    {

    }

    /// <summary>
    /// Private constructor for internal use. Use the static Create method to instantiate a new Client.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="name">The name of the client.</param>
    /// <param name="email">The email of the client.</param>
    private Client(Guid id, string name, Email email)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for internal use. Use the static Create method to instantiate a new Client.
    /// </summary>
    /// <param name="name">The name of the client.</param>
    /// <param name="email">The email of the client.</param>
    private Client(string name, Email email)
        : this(Guid.NewGuid(), name, email)
    {
    }

    /// <summary>
    /// Static factory method to create a new Client instance. Validates the input parameters and throws exceptions if they are invalid.
    /// </summary>
    /// <param name="name">The name of the client.</param>
    /// <param name="email">The email of the client.</param>
    /// <returns>A new instance of the Client class.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the email is null.</exception>
    public static Client Create(string name, Email email)
    {
        // Validate the client's name
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(name, email);

    }

    /// <summary>
    /// Updates the client's name. Validates the new name and throws an exception if it is invalid.
    /// </summary>
    /// <param name="newName">The new name of the client.</param>
    /// <exception cref="ArgumentException">Thrown when the new name is null or whitespace.</exception>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!");
        Name = newName;
    }

    /// <summary>
    /// Updates the client's email. Validates the new email and throws an exception if it is null.
    /// </summary>
    /// <param name="newEmail">The new email of the client.</param>
    /// <exception cref="ArgumentNullException">Thrown when the new email is null.</exception>
    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
    }
}
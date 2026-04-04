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

    private Client()
    {
        
    }
    private Client(Guid id, string name, Email email)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    private Client(string name, Email email)
        : this(Guid.NewGuid(), name, email)
    {
    }

    public static Client Create(string name, Email email)
    {
        // Validate the client's name
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty!");

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return new Client(name, email);

    }


    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty!");
        Name = newName;
    }

    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
    }
}
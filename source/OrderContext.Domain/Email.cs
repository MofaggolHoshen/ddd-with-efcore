using System;
using System.Collections.Generic;
using System.Text;

namespace OrderContext.Domain;

public class Email
{
    private readonly string _value;
    public string Value => _value;

    private Email()
    {
        _value = null!;
    }

    private Email(string value)
    {
        _value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");
        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format!");

        return new Email(email);
    }

}
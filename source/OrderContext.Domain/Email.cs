using OrderContext.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OrderContext.Domain;

public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new Regex(
       @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
       RegexOptions.Compiled | RegexOptions.IgnoreCase
   );

    private readonly string _value;
    public string Value => _value;

    private Email(string value) => _value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty!");

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Invalid email format!");

        if (email.Length > 254)
            throw new ArgumentException("Email exceeds maximum length!");

        return new Email(email);
    }

    // Used by EF Core for materialization - skips validation since data is already validated
    public static Email FromDatabase(string value) => new Email(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

}
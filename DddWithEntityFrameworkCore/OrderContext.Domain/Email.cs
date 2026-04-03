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

    public Email(string value)
    {
        _value = value;
    }

}
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.ValueObjects;

public class Role
{
    public string Value { get; }
    public Role(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Role value cannot be empty.");

        if (value.Length > 50)
            throw new ArgumentException("Role value cannot exceed 50 characters.");

        Value = value;
    }
    public override string ToString() => Value;
}
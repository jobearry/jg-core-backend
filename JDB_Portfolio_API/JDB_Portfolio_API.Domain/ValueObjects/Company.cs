using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.ValueObjects;

public class Company
{
    public string Name { get; }
    public Company(string? name)
    {
        if (name != null && name.Length > 255)
            throw new ArgumentException("Company name cannot exceed 255 characters.");

        Name = string.IsNullOrWhiteSpace(name)? "" : name;
    }
    public override string ToString() => Name;
}

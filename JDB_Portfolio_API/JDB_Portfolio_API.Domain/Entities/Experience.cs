using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Entities;

public class Experience
{
    public int Id { get; }

    public Company? Company { get; }

    public Period Duration { get; private set; }

    public string? Description { get; private set; }

    public Role Role { get; private set; }

    public string? Responsibility { get; private set; }

    public string? Type { get; private set; }
    public List<Project>? Projects { get; set; } = new List<Project>();

    // Domain constructor
    public Experience(int id, Company? company, Period duration, Role role)
    {
        Id = id;
        Company = company;
        Duration = duration;
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }

    // Domain behavior methods
    public void ChangeRole(Role newRole) => Role = newRole ?? throw new ArgumentNullException(nameof(newRole));
    public void UpdateDescription(string description) => Description = description;

    // no plans for responsibility and type yet
}

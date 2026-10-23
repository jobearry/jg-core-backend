using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JDB_Portfolio_API.Domain.Entities;

public class Experience
{
    public int Id { get; }

    /// <summary>
    /// The company the role was held at. Required: plan 3.1a made
    /// <c>company_name</c> <c>NOT NULL</c> with a non-blank <c>CHECK</c>, so a role
    /// always names a company — use "Self-employed" or "Contract — NDA" rather than
    /// leaving it empty.
    /// </summary>
    public Company Company { get; }

    public Period Duration { get; private set; }

    public string? Description { get; private set; }

    /// <summary>
    /// The role held. <c>null</c> means the role is deliberately undeclared — the
    /// owner would rather not state it. It is not an error: the mapping turns a
    /// blank database value into this, and an experience may legitimately exist
    /// without a title.
    /// </summary>
    public Role? Role { get; private set; }

    public string? Responsibility { get; private set; }

    public string? Type { get; private set; }

    /// <summary>
    /// The projects delivered in this role. Read-only since plan 3.3: a public setter let
    /// any caller swap the collection while the aggregate believed nothing had changed.
    /// </summary>
    public IReadOnlyList<Project> Projects { get; private set; } = Array.Empty<Project>();

    // Domain constructor
    public Experience(int id, Company company, Period duration, Role? role)
    {
        Id = id;
        Company = company;
        Duration = duration;
        Role = role;
    }

    // Domain behavior methods
    public void ChangeRole(Role? newRole) => Role = newRole;

    public void UpdateDescription(string? description) => Description = description;

    /// <summary>
    /// Plan 3.3 added these. Before, <c>Responsibility</c> and <c>Type</c> had only a
    /// private setter, so the aggregate could not express two of its own fields from the
    /// outside — but AutoMapper wrote them through reflection anyway, which is the worst
    /// of both worlds: no behaviour, yet no protection either.
    /// </summary>
    public void UpdateResponsibility(string? responsibility) => Responsibility = responsibility;

    public void UpdateType(string? type) => Type = type;

    /// <summary>
    /// Replaces the projects. The argument is copied, so the caller's list cannot mutate
    /// this role afterwards.
    /// </summary>
    public void ReplaceProjects(IEnumerable<Project>? projects) =>
        Projects = projects is null ? Array.Empty<Project>() : projects.ToList();
}

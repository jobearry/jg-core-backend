using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Features.Skill;

/// <summary>
/// The Skill slice's port. The old interface added a redundant
/// <c>GetAllTechStacksAsync</c> over <see cref="IRepository{T}.GetAllAsync"/>; the
/// slice just uses the base method. The overview handler reads through this port too.
/// </summary>
public interface ISkillRepository : IRepository<TechStack>;

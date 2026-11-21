using Portfolio.Domain.Entities;
using Portfolio.Persistence.Entities;
using Portfolio.Persistence.Mappers;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// Test-only dispatcher over the hand-written mappers. It keeps the existing
/// mapping tests expressed as Map&lt;T&gt;(source) while AutoMapper is removed.
/// Production repositories call the mapper classes directly.
/// </summary>
public sealed class HandWrittenMapper
{
    public T Map<T>(object source)
    {
        object result = source switch
        {
            ExperienceEntity entity when typeof(T) == typeof(Experience) =>
                ExperienceMapper.ToDomain(entity),
            Experience domain when typeof(T) == typeof(ExperienceEntity) =>
                ExperienceMapper.ToEntity(domain),
            ProjectEntity entity when typeof(T) == typeof(Project) =>
                ProjectMapper.ToDomain(entity),
            Project domain when typeof(T) == typeof(ProjectEntity) =>
                ProjectMapper.ToEntity(domain),
            TechStackEntity entity when typeof(T) == typeof(TechStack) =>
                TechStackMapper.ToDomain(entity),
            TechStack domain when typeof(T) == typeof(TechStackEntity) =>
                TechStackMapper.ToEntity(domain),
            TechStackCategoryEntity entity when typeof(T) == typeof(TechStackCategory) =>
                TechStackCategoryMapper.ToDomain(entity),
            TechStackCategory domain when typeof(T) == typeof(TechStackCategoryEntity) =>
                TechStackCategoryMapper.ToEntity(domain),
            RecommendationEntity entity when typeof(T) == typeof(Recommendation) =>
                RecommendationMapper.ToDomain(entity),
            Recommendation domain when typeof(T) == typeof(RecommendationEntity) =>
                RecommendationMapper.ToEntity(domain),
            _ => throw new NotSupportedException(
                $"No hand-written mapping from {source.GetType().Name} to {typeof(T).Name}.")
        };

        return (T)result;
    }
}

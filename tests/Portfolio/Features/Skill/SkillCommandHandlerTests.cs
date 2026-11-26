using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.Entities;
using Portfolio.Features.Skill;

namespace Portfolio.Features.Skill.Tests;

/// <summary>
/// Admin write use cases for tech stacks and their categories. Fakes keep the tests
/// on the ports, so no database or HTTP pipeline is involved.
/// </summary>
public class SkillCommandHandlerTests
{
    [Fact]
    public async Task CreateSkill_WithValidRequest_CreatesInTheResolvedCategory()
    {
        var stacks = new FakeTechStackRepository();
        var categories = CategoriesWithBackend();
        var handler = new CreateSkillHandler(
            stacks, categories, NullLogger<CreateSkillHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateSkillRequest("C#", "csharp.svg", "Backend"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("C#", result.Value!.StackName);
        Assert.Equal("Backend", result.Value.Category);
        Assert.Equal("Backend", Assert.Single(stacks.TechStacks).Category);
    }

    [Fact]
    public async Task CreateSkill_WithBlankName_ReturnsValidationError()
    {
        var stacks = new FakeTechStackRepository();
        var handler = new CreateSkillHandler(
            stacks, CategoriesWithBackend(), NullLogger<CreateSkillHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateSkillRequest("  ", null, "Backend"), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Empty(stacks.TechStacks);
    }

    [Fact]
    public async Task CreateSkill_WithUnknownCategory_ReturnsValidationError()
    {
        var stacks = new FakeTechStackRepository();
        var handler = new CreateSkillHandler(
            stacks, CategoriesWithBackend(), NullLogger<CreateSkillHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateSkillRequest("C#", null, "Frontend"), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Empty(stacks.TechStacks);
    }

    [Fact]
    public async Task UpdateSkill_ChangesNameImageAndCategory_KeepsTheId()
    {
        var stacks = new FakeTechStackRepository
        {
            TechStacks = { new TechStack(4, "C#", "old.svg", "Backend") }
        };
        var categories = CategoriesWithBackend();
        var handler = new UpdateSkillHandler(
            stacks, categories, NullLogger<UpdateSkillHandler>.Instance);

        var result = await handler.HandleAsync(
            4, new UpdateSkillRequest("C# 12", "new.svg", "Backend"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Id);
        Assert.Equal("C# 12", result.Value.StackName);
        Assert.Equal("new.svg", result.Value.ImgSrc);
    }

    [Fact]
    public async Task UpdateSkill_ForMissingId_ReturnsNotFound()
    {
        var handler = new UpdateSkillHandler(
            new FakeTechStackRepository(),
            CategoriesWithBackend(),
            NullLogger<UpdateSkillHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new UpdateSkillRequest("C#", null, "Backend"), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task DeleteSkill_WhenUsedByProjects_ReturnsConflict()
    {
        var stacks = new FakeTechStackRepository
        {
            TechStacks = { new TechStack(4, "C#", null, "Backend") },
            IsUsedByProjects = true
        };
        var handler = new DeleteSkillHandler(stacks, NullLogger<DeleteSkillHandler>.Instance);

        var result = await handler.HandleAsync(4, CancellationToken.None);

        AssertFailure(result, ErrorKind.Conflict, "conflict");
        Assert.Single(stacks.TechStacks);
    }

    [Fact]
    public async Task DeleteSkill_WhenUnused_RemovesIt()
    {
        var stacks = new FakeTechStackRepository
        {
            TechStacks = { new TechStack(4, "C#", null, "Backend") }
        };
        var handler = new DeleteSkillHandler(stacks, NullLogger<DeleteSkillHandler>.Instance);

        var result = await handler.HandleAsync(4, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(stacks.TechStacks);
    }

    [Fact]
    public async Task GetSkillById_ForMissingId_ReturnsNotFound()
    {
        var handler = new GetSkillByIdHandler(
            new FakeTechStackRepository(), NullLogger<GetSkillByIdHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task CreateCategory_WithValidRequest_AddsIt()
    {
        var categories = new FakeTechStackCategoryRepository();
        var handler = new CreateSkillCategoryHandler(
            categories, NullLogger<CreateSkillCategoryHandler>.Instance);

        var result = await handler.HandleAsync(
            new SkillCategoryRequest("Frontend"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Frontend", result.Value!.Category);
        Assert.Equal("Frontend", Assert.Single(categories.Categories).Category);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCategory_WithBlankName_ReturnsValidationError(string category)
    {
        var categories = new FakeTechStackCategoryRepository();
        var handler = new CreateSkillCategoryHandler(
            categories, NullLogger<CreateSkillCategoryHandler>.Instance);

        var result = await handler.HandleAsync(
            new SkillCategoryRequest(category), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Empty(categories.Categories);
    }

    [Fact]
    public async Task UpdateCategory_RenamesItAndKeepsTheId()
    {
        var categories = CategoriesWithBackend();
        var handler = new UpdateSkillCategoryHandler(
            categories, NullLogger<UpdateSkillCategoryHandler>.Instance);

        var result = await handler.HandleAsync(
            1, new SkillCategoryRequest("Back-end"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Id);
        Assert.Equal("Back-end", result.Value.Category);
    }

    [Fact]
    public async Task DeleteCategory_WhenItHasStacks_ReturnsConflict()
    {
        var categories = new FakeTechStackCategoryRepository
        {
            Categories = { new TechStackCategory(1, "Backend") },
            HasStacks = true
        };
        var handler = new DeleteSkillCategoryHandler(
            categories, NullLogger<DeleteSkillCategoryHandler>.Instance);

        var result = await handler.HandleAsync(1, CancellationToken.None);

        AssertFailure(result, ErrorKind.Conflict, "conflict");
        Assert.Single(categories.Categories);
    }

    [Fact]
    public async Task DeleteCategory_WhenEmpty_RemovesIt()
    {
        var categories = CategoriesWithBackend();
        var handler = new DeleteSkillCategoryHandler(
            categories, NullLogger<DeleteSkillCategoryHandler>.Instance);

        var result = await handler.HandleAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(categories.Categories);
    }

    [Fact]
    public async Task GetCategoryById_ForMissingId_ReturnsNotFound()
    {
        var handler = new GetSkillCategoryByIdHandler(
            new FakeTechStackCategoryRepository(),
            NullLogger<GetSkillCategoryByIdHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    private static FakeTechStackCategoryRepository CategoriesWithBackend() => new()
    {
        Categories = { new TechStackCategory(1, "Backend") }
    };

    private static void AssertFailure<T>(Result<T> result, ErrorKind kind, string code)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
    }
}

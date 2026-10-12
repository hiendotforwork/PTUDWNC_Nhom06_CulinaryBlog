using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Categories.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CategoryDetailDto?> GetBySlugAsync(
        string slug,
        int page,
        int pageSize,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountRecipesAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<int> CountPublishedRecipesAsync(Guid categoryId, CancellationToken cancellationToken = default);

    void Add(Category category);

    void Update(Category category);

    void SoftDelete(Category category);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

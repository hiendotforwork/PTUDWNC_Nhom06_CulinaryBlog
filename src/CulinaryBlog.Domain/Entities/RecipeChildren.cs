namespace CulinaryBlog.Domain.Entities;

public sealed class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public int OrderIndex { get; set; }
}

public sealed class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int StepNumber { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int? TimerMinutes { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class RecipeImage : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string OriginalUrl { get; set; } = "";
    public string? MediumUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int OrderIndex { get; set; }
}

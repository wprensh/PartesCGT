namespace TiendaVirtual.Api.Domain.Catalog;

public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    /// <summary>1 a 5 estrellas.</summary>
    public int Rating { get; set; }
    public required string Author { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static Review Create(int productId, int rating, string author, string? comment, DateTime createdAt)
    {
        if (rating is < MinRating or > MaxRating)
            throw new ArgumentOutOfRangeException(nameof(rating), rating, $"La calificación va de {MinRating} a {MaxRating}.");

        return new Review
        {
            ProductId = productId,
            Rating = rating,
            Author = author.Trim(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = createdAt
        };
    }
}

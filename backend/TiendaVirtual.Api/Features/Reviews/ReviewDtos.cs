using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Api.Domain.Catalog;

namespace TiendaVirtual.Api.Features.Reviews;

public record ReviewDto(int Id, int Rating, string Author, string? Comment, DateTime CreatedAt)
{
    public static ReviewDto From(Review r) => new(r.Id, r.Rating, r.Author, r.Comment, r.CreatedAt);
}

public record ReviewCreate(
    [Range(Review.MinRating, Review.MaxRating, ErrorMessage = "La calificación va de 1 a 5 estrellas.")] int Rating,
    [Required, StringLength(80, MinimumLength = 2)] string Author,
    [StringLength(1000)] string? Comment);

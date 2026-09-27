using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Domain.Catalog;
using TiendaVirtual.Api.Features.Products;
using TiendaVirtual.Api.Infrastructure.Persistence;

namespace TiendaVirtual.Api.Features.Reviews;

/// <summary>Reseñas públicas. Solo sobre productos publicados (activos).</summary>
public class ReviewService(AppDbContext db, ProductQueries products, TimeProvider clock)
{
    public async Task<Result<List<ReviewDto>>> ListAsync(int productId, CancellationToken ct)
    {
        if (!await products.IsPublishedAsync(productId, ct)) return Error.NotFound();

        return await db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id, r.Rating, r.Author, r.Comment, r.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<Result<ReviewDto>> AddAsync(int productId, ReviewCreate request, CancellationToken ct)
    {
        if (!await products.IsPublishedAsync(productId, ct)) return Error.NotFound();

        var review = Review.Create(productId, request.Rating, request.Author, request.Comment, clock.GetUtcNow().UtcDateTime);
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        return ReviewDto.From(review);
    }
}

namespace Ecommerce.Api.Features.Categories;

using Carter;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Asp.Versioning;

public class CategoryEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/categories")
            .HasApiVersion(1.0)
            .WithTags("Categories");

        group.MapGet("/", async ([AsParameters] PaginationParams query, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            var source = db.Categories.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var t = query.Search.Trim().ToLower();
                source = source.Where(c => c.CategoryName != null && c.CategoryName.ToLower().Contains(t));
            }
            var total = await source.CountAsync();
            var items = await source.OrderBy(c => c.CategoryName).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new CategoryResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName ?? "",
                    CreatedBy = c.CreatedBy,
                    CreatedOn = c.CreatedOn
                }).ToListAsync();
            return Results.Ok(new PaginatedResponse<CategoryResponse>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapPost("/", async (CreateCategoryRequest req, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(req.CategoryName))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "CategoryName is required."
                });
            var e = new Category
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = req.CategoryName.Trim(),
                CreatedBy = req.CreatedBy ?? "system",
                CreatedOn = DateTime.UtcNow
            };
            db.Categories.Add(e);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/categories/{e.CategoryId}", new CategoryResponse
            {
                CategoryId = e.CategoryId, CategoryName = e.CategoryName ?? "",
                CreatedBy = e.CreatedBy, CreatedOn = e.CreatedOn
            });
        });
    }
}

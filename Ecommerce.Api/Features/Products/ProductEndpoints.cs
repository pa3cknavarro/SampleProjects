namespace Ecommerce.Api.Features.Products;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class ProductEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/products")
            .HasApiVersion(1.0)
            .WithTags("Products");

        group.MapGet("/", async ([AsParameters] PaginationParams query, Guid? categoryId, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            IQueryable<Product> source = db.Products.AsNoTracking();
            if (categoryId.HasValue) source = source.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var t = query.Search.Trim().ToLower();
                source = source.Where(p => p.ProductName.ToLower().Contains(t));
            }
            var total = await source.CountAsync();
            var items = await source.OrderBy(p => p.ProductName).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new ProductResponse
                {
                    ProductId = p.ProductId, ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription, CategoryId = p.CategoryId, InStock = p.InStock
                }).ToListAsync();
            return Results.Ok(new PaginatedResponse<ProductResponse>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapPost("/", async (CreateProductRequest req, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(req.ProductName))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "ProductName is required."
                });
            var e = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = req.ProductName.Trim(),
                ProductDescription = req.ProductDescription ?? string.Empty,
                CategoryId = req.CategoryId,
                InStock = req.InStock,
                CreatedBy = req.CreatedBy ?? "system",
                CreatedOn = DateTime.UtcNow
            };
            db.Products.Add(e);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/products/{e.ProductId}", new ProductResponse
            {
                ProductId = e.ProductId, ProductName = e.ProductName,
                ProductDescription = e.ProductDescription, CategoryId = e.CategoryId, InStock = e.InStock
            });
        });

        group.MapGet("/{productId:guid}", async (Guid productId, AppDbContext db, HttpContext http) =>
        {
            var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
            if (p is null)
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Product not found."
                });
            return Results.Ok(new ProductResponse
            {
                ProductId = p.ProductId, ProductName = p.ProductName,
                ProductDescription = p.ProductDescription, CategoryId = p.CategoryId, InStock = p.InStock
            });
        });

        group.MapDelete("/{productId:guid}", async (Guid productId, AppDbContext db, HttpContext http) =>
        {
            var p = await db.Products.FindAsync(productId);
            if (p is null)
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Product not found."
                });
            db.Products.Remove(p);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

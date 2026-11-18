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
            .WithTags("Products")
            .RequireAuthorization("Administrator");

        group.MapGet("/", async ([AsParameters] PaginationParams query, Guid? categoryId, Guid? provinceId, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            IQueryable<Product> source = db.Products.AsNoTracking();
            if (categoryId.HasValue) source = source.Where(p => p.CategoryId == categoryId.Value);
            if (provinceId.HasValue)
                source = source.Where(product => product.AreasAvailable.Any(area =>
                    area.ProvinceId == provinceId.Value && area.IsAvailable));
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
                    ProductDescription = p.ProductDescription, CategoryId = p.CategoryId,
                    ProductImageUrl = p.ProductImageUrl, InStock = p.InStock,
                    DefaultPrice = p.UnitPrices.OrderBy(price => price.UnitOfMeasure.Uom)
                        .Select(price => new ProductPriceResponse(price.UomId, price.UnitOfMeasure.Uom, price.Price))
                        .FirstOrDefault()
                }).ToListAsync();
            return Results.Ok(new PaginatedResponse<ProductResponse>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        }).AllowAnonymous();

        group.MapPost("/", async (CreateProductRequest req, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(req.ProductName))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "ProductName is required."
                });
            var category = await db.Categories.FindAsync(req.CategoryId);
            if (category is null)
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "Category does not exist."
                });
            var e = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = req.ProductName.Trim(),
                ProductDescription = req.ProductDescription ?? string.Empty,
                CategoryId = req.CategoryId,
                ProductImageUrl = req.ProductImageUrl ?? string.Empty,
                InStock = req.InStock,
                Category = category,
                CreatedBy = req.CreatedBy ?? "system",
                CreatedOn = DateTime.UtcNow
            };
            db.Products.Add(e);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/products/{e.ProductId}", new ProductResponse
            {
                ProductId = e.ProductId, ProductName = e.ProductName,
                ProductDescription = e.ProductDescription, CategoryId = e.CategoryId,
                ProductImageUrl = e.ProductImageUrl, InStock = e.InStock
            });
        });

        group.MapGet("/{productId:guid}", async (Guid productId, Guid? provinceId, AppDbContext db, HttpContext http) =>
        {
            var p = await db.Products.AsNoTracking()
                .Where(product => product.ProductId == productId)
                .Where(product => !provinceId.HasValue || product.AreasAvailable.Any(area =>
                    area.ProvinceId == provinceId.Value && area.IsAvailable))
                .Select(product => new ProductResponse
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ProductDescription = product.ProductDescription,
                    CategoryId = product.CategoryId,
                    ProductImageUrl = product.ProductImageUrl,
                    InStock = product.InStock,
                    DefaultPrice = product.UnitPrices.OrderBy(price => price.UnitOfMeasure.Uom)
                        .Select(price => new ProductPriceResponse(price.UomId, price.UnitOfMeasure.Uom, price.Price))
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();
            if (p is null)
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Product not found."
                });
            return Results.Ok(new ProductResponse
            {
                ProductId = p.ProductId, ProductName = p.ProductName,
                ProductDescription = p.ProductDescription, CategoryId = p.CategoryId,
                ProductImageUrl = p.ProductImageUrl, InStock = p.InStock,
                DefaultPrice = p.DefaultPrice
            });
        }).AllowAnonymous();

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
        group.MapPut("/{productId:guid}", async (Guid productId, CreateProductRequest req, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(req.ProductName))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "ProductName is required."
                });
            if (!await db.Categories.AnyAsync(category => category.CategoryId == req.CategoryId))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "Category does not exist."
                });
            var product = await db.Products.FirstOrDefaultAsync(item => item.ProductId == productId);
            if (product is null) return Results.NotFound();
            product.ProductName = req.ProductName.Trim();
            product.ProductDescription = req.ProductDescription ?? string.Empty;
            product.CategoryId = req.CategoryId;
            product.ProductImageUrl = req.ProductImageUrl ?? string.Empty;
            product.InStock = req.InStock;
            await db.SaveChangesAsync();
            return Results.Ok(new ProductResponse
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ProductDescription = product.ProductDescription,
                CategoryId = product.CategoryId,
                ProductImageUrl = product.ProductImageUrl,
                InStock = product.InStock
            });
        });
    }
}

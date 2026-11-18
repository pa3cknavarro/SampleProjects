namespace Ecommerce.Api.Features.UnitPrices;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class UnitPriceResponse
{
    public Guid UnitPriceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    public decimal Price { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateUnitPriceRequest
{
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    public decimal Price { get; set; }
}

public class UpdateUnitPriceRequest { public decimal Price { get; set; } }

public class UnitPriceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi().MapGroup("/api/v{version:apiVersion}/unit-prices").HasApiVersion(1.0).WithTags("UnitPrices");
        group.RequireAuthorization("Administrator");
        group.MapGet("/", GetAllAsync).AllowAnonymous();
        group.MapGet("/{productId:guid}/{uomId:guid}/{unitPriceId:guid}", GetByIdAsync).AllowAnonymous();
        group.MapPost("/", CreateAsync);
        group.MapPut("/{productId:guid}/{uomId:guid}/{unitPriceId:guid}", UpdateAsync);
        group.MapDelete("/{productId:guid}/{uomId:guid}/{unitPriceId:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync([AsParameters] PaginationParams query, Guid? productId, Guid? uomId, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<UnitPrice> source = db.UnitPrices.AsNoTracking().Include(unitPrice => unitPrice.Product)
            .Include(unitPrice => unitPrice.UnitOfMeasure);
        if (productId.HasValue) source = source.Where(u => u.ProductId == productId.Value);
        if (uomId.HasValue) source = source.Where(u => u.UomId == uomId.Value);
        var total = await source.CountAsync();
        var items = await source.OrderBy(u => u.ProductId).ThenBy(u => u.Price).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UnitPriceResponse
            {
                UnitPriceId = u.UnitPriceId, ProductId = u.ProductId, UomId = u.UomId, Price = u.Price,
                ProductName = u.Product.ProductName, Unit = u.UnitOfMeasure.Uom
            }).ToListAsync();
        return Results.Ok(new PaginatedResponse<UnitPriceResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    private static async Task<IResult> GetByIdAsync(Guid productId, Guid uomId, Guid unitPriceId, AppDbContext db, HttpContext http)
    {
        var u = await db.UnitPrices.AsNoTracking().Include(unitPrice => unitPrice.Product)
            .Include(unitPrice => unitPrice.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.UomId == uomId && x.UnitPriceId == unitPriceId);
        if (u is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = "UnitPrice was not found." });
        return Results.Ok(new UnitPriceResponse
        {
            UnitPriceId = u.UnitPriceId, ProductId = u.ProductId, UomId = u.UomId, Price = u.Price,
            ProductName = u.Product.ProductName, Unit = u.UnitOfMeasure.Uom
        });
    }

    private static async Task<IResult> CreateAsync(CreateUnitPriceRequest req, AppDbContext db, HttpContext http)
    {
        if (req.Price < 0) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Price cannot be negative." });
        var product = await db.Products.FirstOrDefaultAsync(item => item.ProductId == req.ProductId);
        if (product is null) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Product does not exist." });
        var unit = await db.UnitOfMeasures.FirstOrDefaultAsync(item => item.Id == req.UomId);
        if (unit is null) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Unit of measure does not exist." });
        if (await db.UnitPrices.AnyAsync(unitPrice => unitPrice.ProductId == req.ProductId && unitPrice.UomId == req.UomId))
            return Results.Conflict(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 409, Title = "Conflict", Detail = "A price is already configured for this product and unit." });
        var entity = new UnitPrice
        {
            UnitPriceId = Guid.NewGuid(),
            ProductId = req.ProductId,
            Product = product,
            UomId = req.UomId,
            UnitOfMeasure = unit,
            Price = req.Price
        };
        db.UnitPrices.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/unit-prices/{entity.ProductId}/{entity.UomId}/{entity.UnitPriceId}", new UnitPriceResponse { UnitPriceId = entity.UnitPriceId, ProductId = entity.ProductId, UomId = entity.UomId, Price = entity.Price });
    }

    private static async Task<IResult> UpdateAsync(Guid productId, Guid uomId, Guid unitPriceId, UpdateUnitPriceRequest req, AppDbContext db, HttpContext http)
    {
        var entity = await db.UnitPrices.FindAsync(productId, uomId, unitPriceId);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = "UnitPrice was not found." });
        if (req.Price < 0) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Price cannot be negative." });
        entity.Price = req.Price;
        await db.SaveChangesAsync();
        return Results.Ok(new UnitPriceResponse { UnitPriceId = entity.UnitPriceId, ProductId = entity.ProductId, UomId = entity.UomId, Price = entity.Price });
    }

    private static async Task<IResult> DeleteAsync(Guid productId, Guid uomId, Guid unitPriceId, AppDbContext db, HttpContext http)
    {
        var entity = await db.UnitPrices.FindAsync(productId, uomId, unitPriceId);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = "UnitPrice was not found." });
        db.UnitPrices.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

namespace Ecommerce.Api.Features.CartItems;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class CartItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateCartItemRequest
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateCartItemRequest
{
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
}

public class CartItemEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi().MapGroup("/api/v{version:apiVersion}/cart-items").HasApiVersion(1.0).WithTags("CartItems");
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync([AsParameters] PaginationParams query, Guid? userId, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<CartItem> source = db.CartItems.AsNoTracking();
        if (userId.HasValue) source = source.Where(c => c.UserId == userId.Value);
        var total = await source.CountAsync();
        var items = await source.OrderByDescending(c => c.CreatedOn).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CartItemResponse { Id = c.Id, UserId = c.UserId, ProductId = c.ProductId, UomId = c.UomId, UnitPrice = c.UnitPrice, Quantity = c.Quantity, CreatedBy = c.CreatedBy, CreatedOn = c.CreatedOn }).ToListAsync();
        return Results.Ok(new PaginatedResponse<CartItemResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var c = await db.CartItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"CartItem '{id}' was not found." });
        return Results.Ok(new CartItemResponse { Id = c.Id, UserId = c.UserId, ProductId = c.ProductId, UomId = c.UomId, UnitPrice = c.UnitPrice, Quantity = c.Quantity, CreatedBy = c.CreatedBy, CreatedOn = c.CreatedOn });
    }

    private static async Task<IResult> CreateAsync(CreateCartItemRequest req, AppDbContext db, HttpContext http)
    {
        if (req.Quantity <= 0) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Quantity must be greater than zero." });
        if (!await db.Users.AnyAsync(u => u.Id == req.UserId)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "User does not exist." });
        if (!await db.Products.AnyAsync(p => p.ProductId == req.ProductId)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Product does not exist." });
        if (!await db.UnitOfMeasures.AnyAsync(u => u.Id == req.UomId)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Unit of measure does not exist." });
        var entity = new CartItem { Id = Guid.NewGuid(), UserId = req.UserId, ProductId = req.ProductId, UomId = req.UomId, UnitPrice = req.UnitPrice, Quantity = req.Quantity, CreatedBy = string.IsNullOrWhiteSpace(req.CreatedBy) ? "system" : req.CreatedBy, CreatedOn = DateTime.UtcNow };
        db.CartItems.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/cart-items/{entity.Id}", new CartItemResponse { Id = entity.Id, UserId = entity.UserId, ProductId = entity.ProductId, UomId = entity.UomId, UnitPrice = entity.UnitPrice, Quantity = entity.Quantity, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn });
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateCartItemRequest req, AppDbContext db, HttpContext http)
    {
        var entity = await db.CartItems.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"CartItem '{id}' was not found." });
        if (req.Quantity <= 0) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Quantity must be greater than zero." });
        entity.UnitPrice = req.UnitPrice;
        entity.Quantity = req.Quantity;
        await db.SaveChangesAsync();
        return Results.Ok(new CartItemResponse { Id = entity.Id, UserId = entity.UserId, ProductId = entity.ProductId, UomId = entity.UomId, UnitPrice = entity.UnitPrice, Quantity = entity.Quantity, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn });
    }

    private static async Task<IResult> DeleteAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var entity = await db.CartItems.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"CartItem '{id}' was not found." });
        db.CartItems.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

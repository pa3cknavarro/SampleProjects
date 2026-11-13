namespace Ecommerce.Api.Features.OrderDetails;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class OrderDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ExtendedPrice { get; set; }
    public decimal ItemFreight { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Comment { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateOrderDetailRequest
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemFreight { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Comment { get; set; }
}

public class UpdateOrderDetailRequest
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemFreight { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Comment { get; set; }
}

public class OrderDetailEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi().MapGroup("/api/v{version:apiVersion}/order-details").HasApiVersion(1.0).WithTags("OrderDetails");
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync([AsParameters] PaginationParams query, Guid? orderId, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<OrderDetail> source = db.OrderDetails.AsNoTracking();
        if (orderId.HasValue) source = source.Where(d => d.OrderId == orderId.Value);
        var total = await source.CountAsync();
        var items = await source.OrderBy(d => d.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new OrderDetailResponse { Id = d.Id, OrderId = d.OrderId, ProductId = d.ProductId, UomId = d.UomId, Quantity = d.Quantity, UnitPrice = d.UnitPrice, ExtendedPrice = d.ExtendedPrice, ItemFreight = d.ItemFreight, DiscountAmount = d.DiscountAmount, Comment = d.Comment }).ToListAsync();
        return Results.Ok(new PaginatedResponse<OrderDetailResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var d = await db.OrderDetails.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"OrderDetail '{id}' was not found." });
        return Results.Ok(new OrderDetailResponse { Id = d.Id, OrderId = d.OrderId, ProductId = d.ProductId, UomId = d.UomId, Quantity = d.Quantity, UnitPrice = d.UnitPrice, ExtendedPrice = d.ExtendedPrice, ItemFreight = d.ItemFreight, DiscountAmount = d.DiscountAmount, Comment = d.Comment });
    }

    private static async Task<IResult> CreateAsync(CreateOrderDetailRequest req, AppDbContext db, HttpContext http)
    {
        if (req.Quantity <= 0) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Quantity must be greater than zero." });
        if (!await db.Orders.AnyAsync(o => o.OrderId == req.OrderId)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Order does not exist." });
        if (!await db.Products.AnyAsync(p => p.ProductId == req.ProductId)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Product does not exist." });
        var extended = Math.Round(req.Quantity * req.UnitPrice - req.DiscountAmount, 2);
        var entity = new OrderDetail { Id = Guid.NewGuid(), OrderId = req.OrderId, ProductId = req.ProductId, UomId = req.UomId, Quantity = req.Quantity, UnitPrice = req.UnitPrice, ExtendedPrice = extended, ItemFreight = req.ItemFreight, DiscountAmount = req.DiscountAmount, Comment = req.Comment };
        db.OrderDetails.Add(entity);
        var order = await db.Orders.FindAsync(req.OrderId);
        if (order is not null) { order.SubTotal += extended; order.GrandTotal = order.SubTotal + order.OrderFreight - order.DiscountAmount; order.ModifiedOn = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/order-details/{entity.Id}", new OrderDetailResponse { Id = entity.Id, OrderId = entity.OrderId, ProductId = entity.ProductId, UomId = entity.UomId, Quantity = entity.Quantity, UnitPrice = entity.UnitPrice, ExtendedPrice = entity.ExtendedPrice, ItemFreight = entity.ItemFreight, DiscountAmount = entity.DiscountAmount, Comment = entity.Comment });
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateOrderDetailRequest req, AppDbContext db, HttpContext http)
    {
        var entity = await db.OrderDetails.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"OrderDetail '{id}' was not found." });
        if (req.Quantity <= 0) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Quantity must be greater than zero." });
        var oldExtended = entity.ExtendedPrice;
        entity.Quantity = req.Quantity; entity.UnitPrice = req.UnitPrice; entity.ItemFreight = req.ItemFreight; entity.DiscountAmount = req.DiscountAmount; entity.Comment = req.Comment;
        entity.ExtendedPrice = Math.Round(req.Quantity * req.UnitPrice - req.DiscountAmount, 2);
        var order = await db.Orders.FindAsync(entity.OrderId);
        if (order is not null) { order.SubTotal = order.SubTotal - oldExtended + entity.ExtendedPrice; order.GrandTotal = order.SubTotal + order.OrderFreight - order.DiscountAmount; order.ModifiedOn = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        return Results.Ok(new OrderDetailResponse { Id = entity.Id, OrderId = entity.OrderId, ProductId = entity.ProductId, UomId = entity.UomId, Quantity = entity.Quantity, UnitPrice = entity.UnitPrice, ExtendedPrice = entity.ExtendedPrice, ItemFreight = entity.ItemFreight, DiscountAmount = entity.DiscountAmount, Comment = entity.Comment });
    }

    private static async Task<IResult> DeleteAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var entity = await db.OrderDetails.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"OrderDetail '{id}' was not found." });
        var order = await db.Orders.FindAsync(entity.OrderId);
        if (order is not null) { order.SubTotal -= entity.ExtendedPrice; order.GrandTotal = order.SubTotal + order.OrderFreight - order.DiscountAmount; order.ModifiedOn = DateTime.UtcNow; }
        db.OrderDetails.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

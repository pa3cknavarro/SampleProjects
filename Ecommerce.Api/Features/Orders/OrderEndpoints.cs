namespace Ecommerce.Api.Features.Orders;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Features.Notifications;
using Ecommerce.Api.Models;
using Ecommerce.Api.Models.Enumerations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public sealed record OrderLineResponse(
    Guid ProductId,
    string ProductName,
    Guid UomId,
    string Unit,
    decimal Quantity,
    decimal UnitPrice);

public sealed record OrderResponse(
    Guid OrderId,
    DateTime OrderDate,
    Guid CustomerId,
    Guid? ShippingProvinceId,
    OrderStatus OrderStatus,
    decimal GrandTotal,
    List<OrderLineResponse> OrderDetail);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    Guid? ShippingProvinceId,
    decimal OrderFreight,
    decimal DiscountAmount,
    string? Comment,
    string? CreatedBy,
    string? ModifiedBy,
    List<CreateOrderLineRequest> OrderDetail);

public sealed record CreateOrderLineRequest(
    Guid ProductId,
    Guid UomId,
    decimal Quantity,
    decimal UnitPrice,
    decimal ItemFreight,
    decimal DiscountAmount,
    string? Comment);

public sealed record UpdateOrderStatusRequest(string OrderStatus);

public class OrderEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/orders")
            .HasApiVersion(1.0)
            .WithTags("Orders");

        group.MapGet("/", async ([AsParameters] PaginationParams query, AppDbContext db, ClaimsPrincipal principal) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            var source = db.Orders.AsNoTracking()
                .Include(o => o.OrderDetail).ThenInclude(detail => detail.Product)
                .Include(o => o.OrderDetail).ThenInclude(detail => detail.UnitOfMeasure)
                .AsQueryable();
            if (!principal.IsInRole("Admin"))
            {
                if (!Guid.TryParse(principal.FindFirstValue("customerId"), out var customerId))
                    return Results.Forbid();
                source = source.Where(order => order.CustomerId == customerId);
            }
            var total = await source.CountAsync();
            var orders = await source.OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new PaginatedResponse<object>
            {
                Items = orders.Select(ToResponse).ToList(), TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapGet("/{orderId:guid}", async (Guid orderId, AppDbContext db, HttpContext http, ClaimsPrincipal principal) =>
        {
            var o = await db.Orders.AsNoTracking()
                .Include(order => order.OrderDetail).ThenInclude(detail => detail.Product)
                .Include(order => order.OrderDetail).ThenInclude(detail => detail.UnitOfMeasure)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (o is not null && !principal.IsInRole("Admin") &&
                (!Guid.TryParse(principal.FindFirstValue("customerId"), out var customerId) || o.CustomerId != customerId))
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Order not found."
                });
            if (o is null)
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Order not found."
                });
            return Results.Ok(ToResponse(o));
        });

        group.MapPost("/", async (CreateOrderRequest request, AppDbContext db, HttpContext http, ClaimsPrincipal principal) =>
        {
            if (request.OrderDetail is null || request.OrderDetail.Count == 0 ||
                request.OrderFreight < 0 || request.DiscountAmount < 0)
                return Results.BadRequest(new { message = "An order requires at least one valid line and nonnegative charges." });

            var customerId = request.CustomerId;
            if (!principal.IsInRole("Admin"))
            {
                if (!Guid.TryParse(principal.FindFirstValue("customerId"), out customerId))
                    return Results.Forbid();
            }

            var customer = await db.Customers.FirstOrDefaultAsync(item => item.CustomerId == customerId);
            if (customer is null)
                return Results.BadRequest(new { message = "The customer does not exist." });

            var shippingProvinceId = request.ShippingProvinceId ?? customer.ProvinceId;
            if (shippingProvinceId.HasValue &&
                !await db.Provinces.AnyAsync(province => province.ProvinceId == shippingProvinceId.Value))
                return Results.BadRequest(new { message = "The shipping province does not exist." });

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                CustomerId = customerId,
                Customer = customer,
                ShippingProvinceId = shippingProvinceId,
                ShippingProvince = shippingProvinceId.HasValue
                    ? await db.Provinces.FirstAsync(province => province.ProvinceId == shippingProvinceId.Value)
                    : null,
                OrderDate = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                OrderFreight = request.OrderFreight,
                DiscountAmount = request.DiscountAmount,
                Comment = request.Comment ?? string.Empty,
                CreatedBy = request.CreatedBy ?? principal.Identity?.Name ?? "system",
                ModifiedBy = request.ModifiedBy ?? principal.Identity?.Name ?? "system"
            };

            foreach (var line in request.OrderDetail)
            {
                if (line.Quantity <= 0 || line.UnitPrice < 0 || line.ItemFreight < 0 || line.DiscountAmount < 0)
                    return Results.BadRequest(new { message = "Order line quantities must be positive and amounts nonnegative." });

                var product = await db.Products.FirstOrDefaultAsync(item => item.ProductId == line.ProductId);
                var unit = await db.UnitOfMeasures.FirstOrDefaultAsync(item => item.Id == line.UomId);
                if (product is null || unit is null)
                    return Results.BadRequest(new { message = "Each order line must reference an existing product and unit." });
                if (!product.InStock)
                    return Results.BadRequest(new { message = "A product in this order is out of stock." });
                var price = await db.UnitPrices
                    .Where(item => item.ProductId == line.ProductId && item.UomId == line.UomId)
                    .Select(item => (decimal?)item.Price)
                    .FirstOrDefaultAsync();
                if (!price.HasValue)
                    return Results.BadRequest(new { message = "Each order line must use a configured product price." });
                if (shippingProvinceId.HasValue &&
                    !await db.AreasAvailable.AnyAsync(area =>
                        area.ProductId == line.ProductId &&
                        area.ProvinceId == shippingProvinceId.Value &&
                        area.IsAvailable))
                    return Results.BadRequest(new { message = "A product in this order is not available in the customer's province." });

                order.OrderDetail.Add(new OrderDetail
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    UomId = unit.Id,
                    Product = product,
                    UnitOfMeasure = unit,
                    Order = order,
                    Quantity = line.Quantity,
                    UnitPrice = price.Value,
                    ItemFreight = line.ItemFreight,
                    DiscountAmount = line.DiscountAmount,
                    ExtendedPrice = line.Quantity * price.Value - line.DiscountAmount,
                    Comment = line.Comment
                });
            }

            order.SubTotal = order.OrderDetail.Sum(line => line.ExtendedPrice);
            order.GrandTotal = order.SubTotal + order.OrderFreight - order.DiscountAmount;
            if (order.GrandTotal < 0)
                return Results.BadRequest(new { message = "Discounts cannot exceed the order total." });

            db.Orders.Add(order);
            await NotificationWriter.AddForCustomerAsync(
                db,
                customerId,
                "Order received",
                $"Your order {order.OrderId} has been placed.",
                http.RequestAborted);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/orders/{order.OrderId}", ToResponse(order));
        });

        group.MapPut("/{orderId:guid}", async (
            Guid orderId,
            UpdateOrderStatusRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            HttpContext http) =>
        {
            if (!Enum.TryParse<OrderStatus>(request.OrderStatus, ignoreCase: true, out var status))
                return Results.BadRequest(new { message = "OrderStatus is invalid." });
            var order = await db.Orders
                .Include(item => item.OrderDetail).ThenInclude(detail => detail.Product)
                .Include(item => item.OrderDetail).ThenInclude(detail => detail.UnitOfMeasure)
                .FirstOrDefaultAsync(item => item.OrderId == orderId);
            if (order is null) return Results.NotFound();
            order.OrderStatus = status;
            order.ModifiedBy = principal.Identity?.Name ?? "administrator";
            order.ModifiedOn = DateTime.UtcNow;
            await NotificationWriter.AddForCustomerAsync(
                db,
                order.CustomerId,
                "Order status updated",
                $"Your order {order.OrderId} is now {status}.",
                http.RequestAborted);
            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(order));
        }).RequireAuthorization("Administrator");
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.OrderId,
        order.OrderDate,
        order.CustomerId,
        order.ShippingProvinceId,
        order.OrderStatus,
        order.GrandTotal,
        order.OrderDetail.Select(detail => new OrderLineResponse(
            detail.ProductId,
            detail.Product?.ProductName ?? string.Empty,
            detail.UomId,
            detail.UnitOfMeasure?.Uom ?? string.Empty,
            detail.Quantity,
            detail.UnitPrice)).ToList());
}

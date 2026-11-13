namespace Ecommerce.Api.Features.Orders;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Ecommerce.Api.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

public class OrderEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/orders")
            .HasApiVersion(1.0)
            .WithTags("Orders");

        group.MapGet("/", async ([AsParameters] PaginationParams query, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            var source = db.Orders.AsNoTracking().Include(o => o.OrderDetail).AsQueryable();
            var total = await source.CountAsync();
            var items = await source.OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new PaginatedResponse<object>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapGet("/{orderId:guid}", async (Guid orderId, AppDbContext db, HttpContext http) =>
        {
            var o = await db.Orders.AsNoTracking().Include(x => x.OrderDetail)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (o is null)
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Order not found."
                });
            return Results.Ok(o);
        });

        group.MapPost("/", async (Order body, AppDbContext db, HttpContext http) =>
        {
            body.OrderId = Guid.NewGuid();
            body.OrderDate = DateTime.UtcNow;
            body.CreatedOn = DateTime.UtcNow;
            body.ModifiedOn = DateTime.UtcNow;
            body.OrderStatus = OrderStatus.Pending;
            if (body.OrderDetail is not null)
            {
                foreach (var d in body.OrderDetail)
                {
                    d.Id = Guid.NewGuid();
                    d.OrderId = body.OrderId;
                    d.ExtendedPrice = d.Quantity * d.UnitPrice - d.DiscountAmount;
                }
                body.SubTotal = body.OrderDetail.Sum(d => d.ExtendedPrice);
                body.GrandTotal = body.SubTotal + body.OrderFreight - body.DiscountAmount;
            }
            db.Orders.Add(body);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/orders/{body.OrderId}", body);
        });
    }
}

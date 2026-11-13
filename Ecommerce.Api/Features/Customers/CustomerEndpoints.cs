namespace Ecommerce.Api.Features.Customers;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class CustomerEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/customers")
            .HasApiVersion(1.0)
            .WithTags("Customers");

        group.MapGet("/", async ([AsParameters] PaginationParams query, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            var source = db.Customers.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var t = query.Search.Trim().ToLower();
                source = source.Where(c => c.CompanyName.ToLower().Contains(t) || c.ContactPerson.ToLower().Contains(t));
            }
            var total = await source.CountAsync();
            var items = await source.OrderBy(c => c.CompanyName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new PaginatedResponse<object>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapPost("/", async (Customer body, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(body.CompanyName))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "CompanyName is required."
                });
            body.CustomerId = Guid.NewGuid();
            body.CreatedOn = DateTime.UtcNow;
            db.Customers.Add(body);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/customers/{body.CustomerId}", body);
        });

        group.MapGet("/{customerId:guid}", async (Guid customerId, AppDbContext db, HttpContext http) =>
        {
            var c = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == customerId);
            if (c is null)
                return Results.NotFound(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                    Detail = "Customer not found."
                });
            return Results.Ok(c);
        });
    }
}

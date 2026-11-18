namespace Ecommerce.Api.Features.CustomerClasses;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class CustomerClassEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/customer-classes")
            .HasApiVersion(1.0)
            .WithTags("CustomerClasses")
            .RequireAuthorization("Administrator");

        group.MapGet("/", async ([AsParameters] PaginationParams query, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            var source = db.CustomerClasses.AsNoTracking().AsQueryable();
            var total = await source.CountAsync();
            var items = await source.OrderBy(c => c.ClassName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new PaginatedResponse<object>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapPost("/", async (CustomerClass body, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(body.ClassName))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "ClassName is required."
                });
            body.CustomerClassId = Guid.NewGuid();
            body.CreatedOn = DateTime.UtcNow;
            db.CustomerClasses.Add(body);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/customer-classes/{body.CustomerClassId}", body);
        });

        group.MapPut("/{id:guid}", async (Guid id, CustomerClass request, AppDbContext db) =>
        {
            var customerClass = await db.CustomerClasses.FindAsync(id);
            if (customerClass is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.ClassName)) return Results.BadRequest(new { message = "ClassName is required." });
            customerClass.ClassName = request.ClassName.Trim();
            customerClass.Description = request.Description?.Trim() ?? string.Empty;
            await db.SaveChangesAsync();
            return Results.Ok(customerClass);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var customerClass = await db.CustomerClasses.FindAsync(id);
            if (customerClass is null) return Results.NotFound();
            db.CustomerClasses.Remove(customerClass);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

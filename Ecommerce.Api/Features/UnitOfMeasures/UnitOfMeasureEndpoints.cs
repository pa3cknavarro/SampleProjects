namespace Ecommerce.Api.Features.UnitOfMeasures;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class UnitOfMeasureEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/unit-of-measures")
            .HasApiVersion(1.0)
            .WithTags("UnitOfMeasures")
            .RequireAuthorization("Administrator");

        group.MapGet("/", async ([AsParameters] PaginationParams query, AppDbContext db) =>
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = Math.Max(query.Page, 1);
            var source = db.UnitOfMeasures.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var t = query.Search.Trim().ToLower();
                source = source.Where(u => u.Uom.ToLower().Contains(t) || u.Description.ToLower().Contains(t));
            }
            var total = await source.CountAsync();
            var items = await source.OrderBy(u => u.Uom).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new PaginatedResponse<object>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            });
        });

        group.MapPost("/", async (UnitOfMeasure body, AppDbContext db, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(body.Uom))
                return Results.BadRequest(new ErrorEnvelope
                {
                    TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                    Detail = "Uom is required."
                });
            body.Id = Guid.NewGuid();
            body.CreatedOn = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(body.CreatedBy)) body.CreatedBy = "system";
            db.UnitOfMeasures.Add(body);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/unit-of-measures/{body.Id}", body);
        });

        group.MapPut("/{id:guid}", async (Guid id, UnitOfMeasure request, AppDbContext db) =>
        {
            var unit = await db.UnitOfMeasures.FindAsync(id);
            if (unit is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.Uom)) return Results.BadRequest(new { message = "Uom is required." });
            unit.Uom = request.Uom.Trim();
            unit.Description = request.Description?.Trim() ?? string.Empty;
            await db.SaveChangesAsync();
            return Results.Ok(unit);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var unit = await db.UnitOfMeasures.FindAsync(id);
            if (unit is null) return Results.NotFound();
            db.UnitOfMeasures.Remove(unit);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

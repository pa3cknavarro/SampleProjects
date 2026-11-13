namespace Ecommerce.Api.Features.Barangays;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class BarangayEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/barangays")
            .HasApiVersion(1.0)
            .WithTags("Barangays");

        group.MapGet("/", GetAsync);
        group.MapGet("/{provinceId:guid}/{cityId:guid}/{barangayId:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{provinceId:guid}/{cityId:guid}/{barangayId:guid}", UpdateAsync);
        group.MapDelete("/{provinceId:guid}/{cityId:guid}/{barangayId:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetAsync(
        [AsParameters] PaginationParams query, Guid? provinceId, Guid? cityId, AppDbContext db)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<Barangay> source = db.Barangays.AsNoTracking();
        if (provinceId.HasValue) source = source.Where(b => b.ProvinceId == provinceId.Value);
        if (cityId.HasValue) source = source.Where(b => b.CityId == cityId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var t = query.Search.Trim().ToLower();
            source = source.Where(b => b.BarangayName.ToLower().Contains(t));
        }
        source = source.OrderBy(b => b.BarangayName);
        var total = await source.CountAsync();
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BarangayResponse
            {
                ProvinceId = b.ProvinceId, CityId = b.CityId, BarangayId = b.BarangayId,
                BarangayName = b.BarangayName, CreatedBy = b.CreatedBy, CreatedOn = b.CreatedOn
            }).ToListAsync();
        return Results.Ok(new PaginatedResponse<BarangayResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    private static async Task<IResult> GetByIdAsync(
        Guid provinceId, Guid cityId, Guid barangayId, AppDbContext db, HttpContext http)
    {
        var b = await db.Barangays.AsNoTracking().FirstOrDefaultAsync(x =>
            x.ProvinceId == provinceId && x.CityId == cityId && x.BarangayId == barangayId);
        if (b is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = "Barangay not found."
            });
        return Results.Ok(new BarangayResponse
        {
            ProvinceId = b.ProvinceId, CityId = b.CityId, BarangayId = b.BarangayId,
            BarangayName = b.BarangayName, CreatedBy = b.CreatedBy, CreatedOn = b.CreatedOn
        });
    }

    private static async Task<IResult> CreateAsync(CreateBarangayRequest req, AppDbContext db, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(req.BarangayName))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                Detail = "BarangayName is required."
            });
        if (!await db.Cities.AnyAsync(c => c.ProvinceId == req.ProvinceId && c.CityId == req.CityId))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                Detail = "City does not exist."
            });
        var entity = new Barangay
        {
            ProvinceId = req.ProvinceId,
            CityId = req.CityId,
            BarangayId = Guid.NewGuid(),
            BarangayName = req.BarangayName.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(req.CreatedBy) ? "system" : req.CreatedBy!,
            CreatedOn = DateTime.UtcNow
        };
        db.Barangays.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/barangays/{entity.ProvinceId}/{entity.CityId}/{entity.BarangayId}",
            new BarangayResponse
            {
                ProvinceId = entity.ProvinceId, CityId = entity.CityId, BarangayId = entity.BarangayId,
                BarangayName = entity.BarangayName, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn
            });
    }

    private static async Task<IResult> UpdateAsync(
        Guid provinceId, Guid cityId, Guid barangayId, UpdateBarangayRequest req, AppDbContext db, HttpContext http)
    {
        var entity = await db.Barangays.FindAsync(provinceId, cityId, barangayId);
        if (entity is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = "Barangay not found."
            });
        entity.BarangayName = req.BarangayName.Trim();
        await db.SaveChangesAsync();
        return Results.Ok(new BarangayResponse
        {
            ProvinceId = entity.ProvinceId, CityId = entity.CityId, BarangayId = entity.BarangayId,
            BarangayName = entity.BarangayName, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn
        });
    }

    private static async Task<IResult> DeleteAsync(
        Guid provinceId, Guid cityId, Guid barangayId, AppDbContext db, HttpContext http)
    {
        var entity = await db.Barangays.FindAsync(provinceId, cityId, barangayId);
        if (entity is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = "Barangay not found."
            });
        db.Barangays.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

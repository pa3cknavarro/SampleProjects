namespace Ecommerce.Api.Features.Cities;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class CityEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/cities")
            .HasApiVersion(1.0)
            .WithTags("Cities")
            .RequireAuthorization("Administrator");

        group.MapGet("/", GetCitiesAsync).AllowAnonymous();
        group.MapGet("/{provinceId:guid}/{cityId:guid}", GetByIdAsync).AllowAnonymous();
        group.MapPost("/", CreateAsync);
        group.MapPut("/{provinceId:guid}/{cityId:guid}", UpdateAsync);
        group.MapDelete("/{provinceId:guid}/{cityId:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetCitiesAsync(
        [AsParameters] PaginationParams query, Guid? provinceId, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<City> source = db.Cities.AsNoTracking();
        if (provinceId.HasValue)
            source = source.Where(c => c.ProvinceId == provinceId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var t = query.Search.Trim().ToLower();
            source = source.Where(c => c.CityName.ToLower().Contains(t));
        }
        source = source.OrderBy(c => c.CityName);
        var total = await source.CountAsync();
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CityResponse
            {
                ProvinceId = c.ProvinceId, CityId = c.CityId, CityName = c.CityName,
                ZipCode = c.ZipCode, CreatedBy = c.CreatedBy, CreatedOn = c.CreatedOn
            }).ToListAsync();
        return Results.Ok(new PaginatedResponse<CityResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    private static async Task<IResult> GetByIdAsync(Guid provinceId, Guid cityId, AppDbContext db, HttpContext http)
    {
        var c = await db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProvinceId == provinceId && x.CityId == cityId);
        if (c is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = "City not found."
            });
        return Results.Ok(new CityResponse
        {
            ProvinceId = c.ProvinceId, CityId = c.CityId, CityName = c.CityName,
            ZipCode = c.ZipCode, CreatedBy = c.CreatedBy, CreatedOn = c.CreatedOn
        });
    }

    private static async Task<IResult> CreateAsync(CreateCityRequest req, AppDbContext db, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(req.CityName))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                Detail = "CityName is required."
            });
        var province = await db.Provinces.FirstOrDefaultAsync(p => p.ProvinceId == req.ProvinceId);
        if (province is null)
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                Detail = "Province does not exist."
            });
        var entity = new City
        {
            ProvinceId = req.ProvinceId,
            CityId = Guid.NewGuid(),
            CityName = req.CityName.Trim(),
            ZipCode = req.ZipCode ?? string.Empty,
            Province = province,
            CreatedBy = string.IsNullOrWhiteSpace(req.CreatedBy) ? "system" : req.CreatedBy!,
            CreatedOn = DateTime.UtcNow
        };
        db.Cities.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/cities/{entity.ProvinceId}/{entity.CityId}", new CityResponse
        {
            ProvinceId = entity.ProvinceId, CityId = entity.CityId, CityName = entity.CityName,
            ZipCode = entity.ZipCode, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn
        });
    }

    private static async Task<IResult> UpdateAsync(Guid provinceId, Guid cityId, UpdateCityRequest req, AppDbContext db, HttpContext http)
    {
        var entity = await db.Cities.FindAsync(provinceId, cityId);
        if (entity is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = "City not found."
            });
        entity.CityName = req.CityName.Trim();
        entity.ZipCode = req.ZipCode ?? string.Empty;
        await db.SaveChangesAsync();
        return Results.Ok(new CityResponse
        {
            ProvinceId = entity.ProvinceId, CityId = entity.CityId, CityName = entity.CityName,
            ZipCode = entity.ZipCode, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn
        });
    }

    private static async Task<IResult> DeleteAsync(Guid provinceId, Guid cityId, AppDbContext db, HttpContext http)
    {
        var entity = await db.Cities.FindAsync(provinceId, cityId);
        if (entity is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = "City not found."
            });
        db.Cities.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

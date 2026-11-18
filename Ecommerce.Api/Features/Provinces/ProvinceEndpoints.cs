namespace Ecommerce.Api.Features.Provinces;

using Carter;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Asp.Versioning;

public class ProvinceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/provinces")
            .HasApiVersion(1.0)
            .WithTags("Provinces")
            .RequireAuthorization("Administrator");

        group.MapGet("/", GetProvincesAsync).WithName("GetProvinces").AllowAnonymous();
        group.MapGet("/{provinceId:guid}", GetByIdAsync).WithName("GetProvinceById").AllowAnonymous();
        group.MapPost("/", CreateAsync).WithName("CreateProvince");
        group.MapPut("/{provinceId:guid}", UpdateAsync).WithName("UpdateProvince");
        group.MapDelete("/{provinceId:guid}", DeleteAsync).WithName("DeleteProvince");
    }

    private static async Task<IResult> GetProvincesAsync(
        [AsParameters] PaginationParams query, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<Province> source = db.Provinces.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            source = source.Where(p => p.ProvinceName.ToLower().Contains(term));
        }
        source = source.OrderBy(p => p.ProvinceName);
        var total = await source.CountAsync();
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProvinceResponse
            {
                ProvinceId = p.ProvinceId,
                ProvinceName = p.ProvinceName,
                CreatedBy = p.CreatedBy,
                CreatedOn = p.CreatedOn
            }).ToListAsync();
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        foreach (var item in items)
        {
            item.Links =
            [
                new HateoasLink { Rel = "self", Href = $"{baseUrl}/api/v1/provinces/{item.ProvinceId}", Method = "GET" }
            ];
        }
        return Results.Ok(new PaginatedResponse<ProvinceResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    private static async Task<IResult> GetByIdAsync(Guid provinceId, AppDbContext db, HttpContext http)
    {
        var p = await db.Provinces.AsNoTracking().FirstOrDefaultAsync(x => x.ProvinceId == provinceId);
        if (p is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = $"Province '{provinceId}' was not found."
            });
        return Results.Ok(new ProvinceResponse
        {
            ProvinceId = p.ProvinceId, ProvinceName = p.ProvinceName,
            CreatedBy = p.CreatedBy, CreatedOn = p.CreatedOn
        });
    }

    private static async Task<IResult> CreateAsync(CreateProvinceRequest request, AppDbContext db, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(request.ProvinceName))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error",
                Detail = "ProvinceName is required."
            });
        var entity = new Province
        {
            ProvinceId = Guid.NewGuid(),
            ProvinceName = request.ProvinceName.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy!,
            CreatedOn = DateTime.UtcNow
        };
        db.Provinces.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/provinces/{entity.ProvinceId}", new ProvinceResponse
        {
            ProvinceId = entity.ProvinceId, ProvinceName = entity.ProvinceName,
            CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn
        });
    }

    private static async Task<IResult> UpdateAsync(Guid provinceId, UpdateProvinceRequest request, AppDbContext db, HttpContext http)
    {
        var entity = await db.Provinces.FindAsync(provinceId);
        if (entity is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = $"Province '{provinceId}' was not found."
            });
        entity.ProvinceName = request.ProvinceName.Trim();
        await db.SaveChangesAsync();
        return Results.Ok(new ProvinceResponse
        {
            ProvinceId = entity.ProvinceId, ProvinceName = entity.ProvinceName,
            CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn
        });
    }

    private static async Task<IResult> DeleteAsync(Guid provinceId, AppDbContext db, HttpContext http)
    {
        var entity = await db.Provinces.FindAsync(provinceId);
        if (entity is null)
            return Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found",
                Detail = $"Province '{provinceId}' was not found."
            });
        db.Provinces.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

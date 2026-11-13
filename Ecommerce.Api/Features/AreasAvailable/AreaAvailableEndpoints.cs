namespace Ecommerce.Api.Features.AreasAvailable;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public class AreaAvailableResponse
{
    public Guid AreaAvailableId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProvinceId { get; set; }
    public bool IsAvailable { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateAreaAvailableRequest
{
    public Guid ProductId { get; set; }
    public Guid ProvinceId { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class UpdateAreaAvailableRequest { public bool IsAvailable { get; set; } }

public class AreaAvailableEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/areas-available")
            .HasApiVersion(1.0).WithTags("AreasAvailable");
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync([AsParameters] PaginationParams query, Guid? productId, Guid? provinceId, bool? isAvailable, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<AreaAvailable> source = db.AreasAvailable.AsNoTracking();
        if (productId.HasValue) source = source.Where(a => a.ProductId == productId.Value);
        if (provinceId.HasValue) source = source.Where(a => a.ProvinceId == provinceId.Value);
        if (isAvailable.HasValue) source = source.Where(a => a.IsAvailable == isAvailable.Value);
        var total = await source.CountAsync();
        var items = await source.OrderBy(a => a.ProductId).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AreaAvailableResponse { AreaAvailableId = a.AreaAvailableId, ProductId = a.ProductId, ProvinceId = a.ProvinceId, IsAvailable = a.IsAvailable }).ToListAsync();
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        foreach (var item in items)
            item.Links = [ new HateoasLink { Rel = "self", Href = $"{baseUrl}/api/v1/areas-available/{item.AreaAvailableId}", Method = "GET" } ];
        return Results.Ok(new PaginatedResponse<AreaAvailableResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var a = await db.AreasAvailable.AsNoTracking().FirstOrDefaultAsync(x => x.AreaAvailableId == id);
        if (a is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"AreaAvailable '{id}' was not found." });
        return Results.Ok(new AreaAvailableResponse { AreaAvailableId = a.AreaAvailableId, ProductId = a.ProductId, ProvinceId = a.ProvinceId, IsAvailable = a.IsAvailable });
    }

    private static async Task<IResult> CreateAsync(CreateAreaAvailableRequest req, AppDbContext db, HttpContext http)
    {
        if (!await db.Products.AnyAsync(p => p.ProductId == req.ProductId))
            return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Product does not exist." });
        if (!await db.Provinces.AnyAsync(p => p.ProvinceId == req.ProvinceId))
            return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Province does not exist." });
        var entity = new AreaAvailable { AreaAvailableId = Guid.NewGuid(), ProductId = req.ProductId, ProvinceId = req.ProvinceId, IsAvailable = req.IsAvailable };
        db.AreasAvailable.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/areas-available/{entity.AreaAvailableId}", new AreaAvailableResponse { AreaAvailableId = entity.AreaAvailableId, ProductId = entity.ProductId, ProvinceId = entity.ProvinceId, IsAvailable = entity.IsAvailable });
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateAreaAvailableRequest req, AppDbContext db, HttpContext http)
    {
        var entity = await db.AreasAvailable.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"AreaAvailable '{id}' was not found." });
        entity.IsAvailable = req.IsAvailable;
        await db.SaveChangesAsync();
        return Results.Ok(new AreaAvailableResponse { AreaAvailableId = entity.AreaAvailableId, ProductId = entity.ProductId, ProvinceId = entity.ProvinceId, IsAvailable = entity.IsAvailable });
    }

    private static async Task<IResult> DeleteAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var entity = await db.AreasAvailable.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"AreaAvailable '{id}' was not found." });
        db.AreasAvailable.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

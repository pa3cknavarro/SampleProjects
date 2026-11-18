namespace Ecommerce.Api.Features.Users;

using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Ecommerce.Api.Models.Enumerations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

public class UserResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? CustomerId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateUserRequest
{
    public Guid CompanyId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Administrative user management API. Passwords are stored only as one-way hashes.</summary>
public class UserEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi().MapGroup("/api/v{version:apiVersion}/users").HasApiVersion(1.0).WithTags("Users");
        group.RequireAuthorization("Administrator");
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync([AsParameters] PaginationParams query, bool? isActive, AppDbContext db, HttpContext http)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        IQueryable<User> source = db.Users.AsNoTracking();
        if (isActive.HasValue) source = source.Where(u => u.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var t = query.Search.Trim().ToLower();
            source = source.Where(u => u.Username.ToLower().Contains(t));
        }
        var total = await source.CountAsync();
        var items = await source.OrderBy(u => u.Username).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserResponse { Id = u.Id, CompanyId = u.CompanyId, Username = u.Username, Email = u.Email, Role = u.Role.ToString(), IsActive = u.IsActive, CustomerId = u.CustomerId, CreatedBy = u.CreatedBy, CreatedOn = u.CreatedOn }).ToListAsync();
        return Results.Ok(new PaginatedResponse<UserResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"User '{id}' was not found." });
        return Results.Ok(new UserResponse { Id = u.Id, CompanyId = u.CompanyId, Username = u.Username, Email = u.Email, Role = u.Role.ToString(), IsActive = u.IsActive, CustomerId = u.CustomerId, CreatedBy = u.CreatedBy, CreatedOn = u.CreatedOn });
    }

    private static async Task<IResult> CreateAsync(CreateUserRequest req, AppDbContext db, IPasswordHasher<User> passwordHasher, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Username is required." });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Password must be at least 8 characters." });
        if (string.IsNullOrWhiteSpace(req.Email)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Email is required." });
        if (await db.Users.AnyAsync(u => u.Username == req.Username.Trim() || u.Email == req.Email.Trim())) return Results.Conflict(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 409, Title = "Conflict", Detail = "Username or email already exists." });
        if (!Enum.TryParse<Roles>(req.Role, true, out var role)) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Role is invalid." });
        var customerId = await db.Customers.AnyAsync(customer => customer.CustomerId == req.CompanyId)
            ? req.CompanyId
            : (Guid?)null;
        var entity = new User { Id = Guid.NewGuid(), CompanyId = req.CompanyId, CustomerId = customerId, Username = req.Username.Trim(), Email = req.Email.Trim().ToLowerInvariant(), Role = role, IsActive = req.IsActive, CreatedBy = string.IsNullOrWhiteSpace(req.CreatedBy) ? "system" : req.CreatedBy!, CreatedOn = DateTime.UtcNow };
        entity.PasswordHash = passwordHasher.HashPassword(entity, req.Password);
        db.Users.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/users/{entity.Id}", new UserResponse { Id = entity.Id, CompanyId = entity.CompanyId, Username = entity.Username, Email = entity.Email, Role = entity.Role.ToString(), IsActive = entity.IsActive, CustomerId = entity.CustomerId, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn });
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateUserRequest req, AppDbContext db, IPasswordHasher<User> passwordHasher, HttpContext http)
    {
        var entity = await db.Users.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"User '{id}' was not found." });
        if (!string.IsNullOrWhiteSpace(req.Email)) entity.Email = req.Email.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            if (req.Password.Length < 8) return Results.BadRequest(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 400, Title = "Validation Error", Detail = "Password must be at least 8 characters." });
            entity.PasswordHash = passwordHasher.HashPassword(entity, req.Password);
        }
        if (!string.IsNullOrWhiteSpace(req.Role) && Enum.TryParse<Roles>(req.Role, true, out var role)) entity.Role = role;
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        await db.SaveChangesAsync();
        return Results.Ok(new UserResponse { Id = entity.Id, CompanyId = entity.CompanyId, Username = entity.Username, Email = entity.Email, Role = entity.Role.ToString(), IsActive = entity.IsActive, CustomerId = entity.CustomerId, CreatedBy = entity.CreatedBy, CreatedOn = entity.CreatedOn });
    }

    private static async Task<IResult> DeleteAsync(Guid id, AppDbContext db, HttpContext http)
    {
        var entity = await db.Users.FindAsync(id);
        if (entity is null) return Results.NotFound(new ErrorEnvelope { TraceId = http.TraceIdentifier, Status = 404, Title = "Not Found", Detail = $"User '{id}' was not found." });
        db.Users.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

namespace Ecommerce.Api.Features.Customers;

using System.Security.Claims;
using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public sealed record UpdateCustomerProfileRequest(string EmailAddress, string? ContactNumber);

public sealed record CustomerProfileResponse(
    Guid CustomerId,
    string CompanyName,
    string ContactPerson,
    string EmailAddress,
    string ContactNumber,
    object? CustomerClass,
    object? Province,
    object? City,
    object? Barangay,
    string Address,
    Guid? ProvinceId);

public sealed record CustomerAdminResponse(
    Guid CustomerId,
    string CompanyName,
    Guid CustomerClassId,
    string ContactPerson,
    string EmailAddress,
    string ContactNumber,
    string Address,
    bool IsActive);

public sealed record CreateCustomerRequest(
    string CompanyName,
    Guid CustomerClassId,
    string? ContactPerson,
    string? EmailAddress,
    string? ContactNumber,
    string? Address);

public class CustomerEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/customers")
            .HasApiVersion(1.0)
            .WithTags("Customers");

        group.MapGet("/me", GetMyProfileAsync);
        group.MapPut("/me", UpdateMyProfileAsync);
        group.MapGet("/", GetAllAsync).RequireAuthorization("Administrator");
        group.MapPost("/", CreateAsync).RequireAuthorization("Administrator");
        group.MapGet("/{customerId:guid}", GetByIdAsync).RequireAuthorization("Administrator");
        group.MapPut("/{customerId:guid}", UpdateAsync).RequireAuthorization("Administrator");
        group.MapDelete("/{customerId:guid}", DeleteAsync).RequireAuthorization("Administrator");
    }

    private static async Task<IResult> GetMyProfileAsync(ClaimsPrincipal principal, AppDbContext db)
    {
        if (!Guid.TryParse(principal.FindFirstValue("customerId"), out var customerId))
            return Results.NotFound(new { message = "No customer record is linked to this account." });

        var customer = await db.Customers.AsNoTracking()
            .Include(c => c.CustomerClass)
            .Include(c => c.Province)
            .Include(c => c.City)
            .Include(c => c.Barangay)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        return customer is null ? Results.NotFound() : Results.Ok(ToProfileResponse(customer));
    }

    private static async Task<IResult> UpdateMyProfileAsync(
        UpdateCustomerProfileRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        HttpContext http)
    {
        if (!Guid.TryParse(principal.FindFirstValue("customerId"), out var customerId))
            return Results.NotFound(new { message = "No customer record is linked to this account." });
        if (string.IsNullOrWhiteSpace(request.EmailAddress))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier,
                Status = 400,
                Title = "Validation Error",
                Detail = "Email address is required."
            });

        var customer = await db.Customers
            .Include(c => c.CustomerClass)
            .Include(c => c.Province)
            .Include(c => c.City)
            .Include(c => c.Barangay)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer is null) return Results.NotFound();

        customer.EmailAddress = request.EmailAddress.Trim();
        customer.ContactNumber = request.ContactNumber?.Trim() ?? string.Empty;
        await db.SaveChangesAsync();
        return Results.Ok(ToProfileResponse(customer));
    }

    private static async Task<IResult> GetAllAsync([AsParameters] PaginationParams query, AppDbContext db)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        var source = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            source = source.Where(c => c.CompanyName.ToLower().Contains(term) || c.ContactPerson.ToLower().Contains(term));
        }

        var total = await source.CountAsync();
        var items = await source.OrderBy(c => c.CompanyName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CustomerAdminResponse(
                c.CustomerId,
                c.CompanyName,
                c.CustomerClassId,
                c.ContactPerson,
                c.EmailAddress,
                c.ContactNumber,
                c.Address,
                c.IsActive))
            .ToListAsync();
        return Results.Ok(new PaginatedResponse<CustomerAdminResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    private static async Task<IResult> CreateAsync(CreateCustomerRequest request, AppDbContext db, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier,
                Status = 400,
                Title = "Validation Error",
                Detail = "CompanyName is required."
            });

        var customerClass = await db.CustomerClasses.FindAsync(request.CustomerClassId);
        if (customerClass is null)
            return Results.BadRequest(new { message = "CustomerClassId must identify an existing class." });

        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            CompanyName = request.CompanyName.Trim(),
            CustomerClassId = customerClass.CustomerClassId,
            CustomerClass = customerClass,
            ContactPerson = request.ContactPerson?.Trim() ?? string.Empty,
            EmailAddress = request.EmailAddress?.Trim() ?? string.Empty,
            ContactNumber = request.ContactNumber?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            Province = null!,
            City = null!,
            Barangay = null!,
            CreatedOn = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/customers/{customer.CustomerId}", ToAdminResponse(customer));
    }

    private static async Task<IResult> GetByIdAsync(Guid customerId, AppDbContext db, HttpContext http)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        return customer is null
            ? Results.NotFound(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier,
                Status = 404,
                Title = "Not Found",
                Detail = "Customer not found."
            })
            : Results.Ok(ToAdminResponse(customer));
    }

    private static async Task<IResult> UpdateAsync(
        Guid customerId,
        CreateCustomerRequest request,
        AppDbContext db,
        HttpContext http)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return Results.BadRequest(new ErrorEnvelope
            {
                TraceId = http.TraceIdentifier,
                Status = 400,
                Title = "Validation Error",
                Detail = "CompanyName is required."
            });
        if (!await db.CustomerClasses.AnyAsync(c => c.CustomerClassId == request.CustomerClassId))
            return Results.BadRequest(new { message = "CustomerClassId must identify an existing class." });

        customer.CompanyName = request.CompanyName.Trim();
        customer.CustomerClassId = request.CustomerClassId;
        customer.ContactPerson = request.ContactPerson?.Trim() ?? string.Empty;
        customer.EmailAddress = request.EmailAddress?.Trim() ?? string.Empty;
        customer.ContactNumber = request.ContactNumber?.Trim() ?? string.Empty;
        customer.Address = request.Address?.Trim() ?? string.Empty;
        await db.SaveChangesAsync();
        return Results.Ok(ToAdminResponse(customer));
    }

    private static async Task<IResult> DeleteAsync(Guid customerId, AppDbContext db)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return Results.NotFound();
        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static CustomerProfileResponse ToProfileResponse(Customer customer) => new(
        customer.CustomerId,
        customer.CompanyName,
        customer.ContactPerson,
        customer.EmailAddress,
        customer.ContactNumber,
        new { className = customer.CustomerClass?.ClassName ?? string.Empty },
        customer.ProvinceId.HasValue && customer.Province is not null
            ? new { provinceName = customer.Province.ProvinceName }
            : null,
        customer.CityId.HasValue && customer.City is not null
            ? new { cityName = customer.City.CityName }
            : null,
        customer.BarangayId.HasValue && customer.Barangay is not null
            ? new { barangayName = customer.Barangay.BarangayName }
            : null,
        customer.Address,
        customer.ProvinceId);

    private static CustomerAdminResponse ToAdminResponse(Customer customer) => new(
        customer.CustomerId,
        customer.CompanyName,
        customer.CustomerClassId,
        customer.ContactPerson,
        customer.EmailAddress,
        customer.ContactNumber,
        customer.Address,
        customer.IsActive);
}

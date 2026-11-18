namespace Ecommerce.Api.Features.Notifications;

using System.Security.Claims;
using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

public sealed class NotificationEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/notifications")
            .HasApiVersion(1.0)
            .WithTags("Notifications");

        group.MapGet("/", ListAsync);
        group.MapPost("/{notificationId:guid}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] PaginationParams query,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = db.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);
        var total = await source.CountAsync(cancellationToken);
        var unreadCount = await source.CountAsync(notification => notification.ReadOn == null, cancellationToken);
        var items = await source.OrderByDescending(notification => notification.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(notification => new NotificationResponse(
                notification.NotificationId,
                notification.Title,
                notification.Message,
                notification.CreatedOn,
                notification.ReadOn))
            .ToListAsync(cancellationToken);

        return Results.Ok(new NotificationListResponse(
            items,
            total,
            page,
            pageSize,
            (int)Math.Ceiling(total / (double)pageSize),
            unreadCount));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid notificationId,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var notification = await db.Notifications.FirstOrDefaultAsync(
            item => item.NotificationId == notificationId && item.UserId == userId,
            cancellationToken);
        if (notification is null)
        {
            return Results.NotFound();
        }

        notification.ReadOn ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var unread = await db.Notifications
            .Where(item => item.UserId == userId && item.ReadOn == null)
            .ToListAsync(cancellationToken);
        var readOn = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.ReadOn = readOn;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public sealed record NotificationResponse(
    Guid NotificationId,
    string Title,
    string Message,
    DateTime CreatedOn,
    DateTime? ReadOn);

public sealed record NotificationListResponse(
    List<NotificationResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int UnreadCount);

public static class NotificationWriter
{
    public static async Task AddForCustomerAsync(
        AppDbContext db,
        Guid customerId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var userIds = await db.Users
            .Where(user => user.CustomerId == customerId && user.IsActive)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        AddForUsers(db, userIds, title, message);
    }

    public static void AddForUsers(AppDbContext db, IEnumerable<Guid> userIds, string title, string message)
    {
        var createdOn = DateTime.UtcNow;
        db.Notifications.AddRange(userIds.Select(userId => new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            CreatedOn = createdOn
        }));
    }
}

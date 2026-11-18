namespace Ecommerce.Api.Features.Auth;

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Claims;
using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Features.Notifications;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public sealed class AuthEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/auth")
            .HasApiVersion(1.0)
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/verify-email", VerifyEmailAsync).AllowAnonymous();
        group.MapPost("/resend-verification", ResendVerificationAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("AuthSensitive");
        group.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous();
        group.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous();
        group.MapGet("/me", GetCurrentUserAsync);
        group.MapPost("/logout", (Delegate)LogoutAsync);
        group.MapPost("/change-password", ChangePasswordAsync);
        group.MapGet("/mfa", GetMfaStatusAsync);
        group.MapPost("/mfa/setup", SetupMfaAsync);
        group.MapPost("/mfa/enable", EnableMfaAsync).RequireRateLimiting("AuthSensitive");
        group.MapPost("/mfa/disable", DisableMfaAsync).RequireRateLimiting("AuthSensitive");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        CredentialFlowStore flowStore,
        IEmailSender emailSender,
        IWebHostEnvironment environment,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.CompanyName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Error(http, 400, "Validation Error", "Company name, email, and password are required.");
        }

        var email = NormalizeEmail(request.Email);
        if (!new EmailAddressAttribute().IsValid(email) ||
            request.Password.Length < 8)
        {
            return Error(http, 400, "Validation Error", "Enter a valid email address and a password of at least 8 characters.");
        }

        if (await db.Users.AnyAsync(user => user.Email == email || user.Username == email, cancellationToken))
        {
            return Error(http, 409, "Conflict", "An account with that email already exists.");
        }

        var candidate = new User { Username = email, Email = email };
        var passwordHash = passwordHasher.HashPassword(candidate, request.Password);
        var code = flowStore.BeginRegistration(email, email, request.CompanyName.Trim(), passwordHash);
        return await DeliverChallengeAsync(emailSender, environment, email, "Verify your account",
            $"Your Ecommerce verification code is {code}. It expires in 15 minutes.",
            code, "verificationCode", cancellationToken);
    }

    private static async Task<IResult> VerifyEmailAsync(
        VerifyEmailRequest request,
        AppDbContext db,
        CredentialFlowStore flowStore,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Error(http, 400, "Verification Failed", "Email and verification code are required.");
        }

        var email = NormalizeEmail(request.Email);
        var customerClass = await db.CustomerClasses
            .FirstOrDefaultAsync(cancellationToken);
        if (customerClass is null)
        {
            return Error(http, 503, "Registration Unavailable", "Customer registration is not configured on this server.");
        }

        if (!flowStore.TryCompleteRegistration(email, request.Code, out var pending))
        {
            return Error(http, 400, "Verification Failed", "The code is invalid or expired. Request a new code and try again.");
        }

        var customerId = Guid.NewGuid();
        var customer = new Customer
        {
            CustomerId = customerId,
            CompanyName = pending.CompanyName,
            CustomerClassId = customerClass.CustomerClassId,
            CustomerClass = customerClass,
            ContactPerson = pending.Username,
            EmailAddress = pending.Email,
            IsActive = true,
            CreatedBy = pending.Username,
            CreatedOn = DateTime.UtcNow,
            Province = null!,
            City = null!,
            Barangay = null!
        };
        db.Customers.Add(customer);
        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = customerId,
            Username = pending.Username,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            CustomerId = customerId,
            Role = Ecommerce.Api.Models.Enumerations.Roles.User,
            IsActive = true,
            CreatedBy = pending.Username,
            CreatedOn = DateTime.UtcNow
        };
        db.Users.Add(user);
        NotificationWriter.AddForUsers(db, [user.Id], "Welcome", "Your Ecommerce account is ready.");
        await db.SaveChangesAsync(cancellationToken);
        await SignInAsync(http, user, persistent: false);
        return Results.Ok(ToResponse(user));
    }

    private static async Task<IResult> ResendVerificationAsync(
        EmailRequest request,
        CredentialFlowStore flowStore,
        IEmailSender emailSender,
        IWebHostEnvironment environment,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Error(http, 400, "Validation Error", "Email is required.");
        }

        var email = NormalizeEmail(request.Email);
        var code = flowStore.ResendRegistrationCode(email);
        if (code is not null)
        {
            return await DeliverChallengeAsync(emailSender, environment, email, "Verify your account",
                $"Your Ecommerce verification code is {code}. It expires in 15 minutes.",
                code, "verificationCode", cancellationToken);
        }

        return Results.Ok(new { message = "If a registration is pending, a new verification code has been sent." });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        TotpService totpService,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Error(http, 400, "Validation Error", "Username and password are required.");
        }

        var identifier = request.Username.Trim();
        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Username == identifier || candidate.Email == NormalizeEmail(identifier),
            cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Error(http, 401, "Unauthorized", "Username or password is incorrect.");
        }

        if (user.MfaEnabled && !IsValidMfaCode(user, request.TwoFactorCode, totpService))
        {
            if (!await TryConsumeRecoveryCodeAsync(db, user.Id, request.TwoFactorCode, cancellationToken))
            {
                return Results.Json(new
                {
                    status = StatusCodes.Status401Unauthorized,
                    title = "Two-factor verification required",
                    detail = "Enter an authenticator or unused recovery code.",
                    requiresTwoFactor = true
                }, statusCode: StatusCodes.Status401Unauthorized);
            }

        }

        await SignInAsync(http, user, request.RememberMe);
        return Results.Ok(ToResponse(user));
    }

    private static async Task<IResult> GetMfaStatusAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, db, cancellationToken);
        return user is null ? Results.Unauthorized() : Results.Ok(new { enabled = user.MfaEnabled });
    }

    private static async Task<IResult> SetupMfaAsync(
        MfaSetupRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        TotpService totpService,
        CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (user.MfaEnabled)
        {
            return Results.Conflict(new { message = "Two-factor authentication is already enabled." });
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Results.BadRequest(new { message = "Current password is required." });
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return Results.BadRequest(new { message = "Current password is incorrect." });
        }

        var secret = totpService.CreateSecret();
        user.MfaSecretProtected = totpService.Protect(secret);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new
        {
            secret,
            provisioningUri = totpService.CreateProvisioningUri(user.Email, secret)
        });
    }

    private static async Task<IResult> EnableMfaAsync(
        OneTimeCodeRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        TotpService totpService,
        CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (user.MfaSecretProtected is null || !IsValidMfaCode(user, request.Code, totpService))
        {
            return Results.BadRequest(new { message = "The code is invalid or setup has not been started." });
        }

        user.MfaEnabled = true;
        var recoveryCodes = Enumerable.Range(0, 10)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(10)))
            .ToArray();
        db.MfaRecoveryCodes.AddRange(recoveryCodes.Select(code => new MfaRecoveryCode
        {
            RecoveryCodeId = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = HashRecoveryCode(code)
        }));
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { enabled = true, recoveryCodes });
    }

    private static async Task<IResult> DisableMfaAsync(
        OneTimeCodeRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        TotpService totpService,
        CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!user.MfaEnabled || !IsValidMfaCode(user, request.Code, totpService))
        {
            return Results.BadRequest(new { message = "The code is invalid or two-factor authentication is not enabled." });
        }

        user.MfaEnabled = false;
        user.MfaSecretProtected = null;
        var recoveryCodes = await db.MfaRecoveryCodes
            .Where(code => code.UserId == user.Id)
            .ToListAsync(cancellationToken);
        db.MfaRecoveryCodes.RemoveRange(recoveryCodes);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { enabled = false });
    }

    private static async Task<User?> FindCurrentUserAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            : null;
    }

    private static bool IsValidMfaCode(User user, string? code, TotpService totpService)
    {
        if (user.MfaSecretProtected is null)
        {
            return false;
        }

        try
        {
            return totpService.Verify(totpService.Unprotect(user.MfaSecretProtected), code);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }

    private static async Task<bool> TryConsumeRecoveryCodeAsync(
        AppDbContext db,
        Guid userId,
        string? code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var hash = HashRecoveryCode(code);
        return await db.MfaRecoveryCodes
            .Where(item => item.UserId == userId && item.CodeHash == hash)
            .ExecuteDeleteAsync(cancellationToken) == 1;
    }

    private static string HashRecoveryCode(string code) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant())));

    private static async Task<IResult> ForgotPasswordAsync(
        EmailRequest request,
        AppDbContext db,
        CredentialFlowStore flowStore,
        IEmailSender emailSender,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Error(http, 400, "Validation Error", "Email is required.");
        }

        if (!emailSender.IsConfigured && !environment.IsDevelopment())
        {
            return Error(http, 503, "Email Unavailable", "Password recovery is not configured on this server.");
        }

        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (user is null)
        {
            return Results.Ok(new { message = "If the account exists, password recovery instructions have been sent." });
        }

        var token = flowStore.CreatePasswordReset(user.Id);
        var baseUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        if (emailSender.IsConfigured)
        {
            await emailSender.SendAsync(email, "Reset your password", $"Use this link within 15 minutes: {resetUrl}", cancellationToken);
        }

        var response = new Dictionary<string, string> { ["message"] = "If the account exists, password recovery instructions have been sent." };
        if (environment.IsDevelopment())
        {
            response["resetUrl"] = resetUrl;
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        AppDbContext db,
        CredentialFlowStore flowStore,
        IPasswordHasher<User> passwordHasher,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword) ||
            request.NewPassword.Length < 8)
        {
            return Error(http, 400, "Validation Error", "Password must be at least 8 characters.");
        }

        if (!flowStore.TryConsumePasswordReset(request.Token, out var userId))
        {
            return Error(http, 400, "Reset Failed", "The reset link is invalid or expired.");
        }

        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return Error(http, 400, "Reset Failed", "The reset link is invalid or expired.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(AppDbContext db, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        return user is null ? Results.Unauthorized() : Results.Ok(ToResponse(user));
    }

    private static async Task<IResult> LogoutAsync(HttpContext http)
    {
        await http.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return Results.BadRequest(new { message = "Current password is incorrect." });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return Results.BadRequest(new { message = "Password must be at least 8 characters." });
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeliverChallengeAsync(
        IEmailSender emailSender,
        IWebHostEnvironment environment,
        string email,
        string subject,
        string body,
        string code,
        string responseProperty,
        CancellationToken cancellationToken)
    {
        if (emailSender.IsConfigured)
        {
            await emailSender.SendAsync(email, subject, body, cancellationToken);
            return Results.Accepted(value: new { message = "If registration can be completed, a verification code has been sent." });
        }

        if (!environment.IsDevelopment())
        {
            return Results.Json(
                new { message = "Email delivery is not configured." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new Dictionary<string, string>
        {
            ["message"] = "Development verification code.",
            [responseProperty] = code
        });
    }

    private static async Task SignInAsync(HttpContext http, User user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.CustomerId.HasValue)
        {
            claims.Add(new Claim("customerId", user.CustomerId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "EcommerceCookie");
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent });
    }

    private static AuthUserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        CompanyId = user.CompanyId,
        CustomerId = user.CustomerId,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        IsActive = user.IsActive
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static IResult Error(HttpContext http, int status, string title, string detail) =>
        Results.Json(new ErrorEnvelope
        {
            TraceId = http.TraceIdentifier,
            Status = status,
            Title = title,
            Detail = detail
        }, statusCode: status);
}

public sealed record RegisterRequest(string Email, string Password, string CompanyName);
public sealed record VerifyEmailRequest(string Email, string Code);
public sealed record EmailRequest(string Email);
public sealed record LoginRequest(string Username, string Password, bool RememberMe = false, string? TwoFactorCode = null);
public sealed record OneTimeCodeRequest(string Code);
public sealed record MfaSetupRequest(string CurrentPassword);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed class AuthUserResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

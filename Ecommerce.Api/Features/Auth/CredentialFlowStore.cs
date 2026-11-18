namespace Ecommerce.Api.Features.Auth;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

public sealed class CredentialFlowStore
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, PendingRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PasswordResetTicket> _passwordResets = new(StringComparer.Ordinal);

    public string BeginRegistration(string email, string username, string companyName, string passwordHash)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _registrations[email] = new PendingRegistration(username, email, companyName, passwordHash, Hash(code), DateTimeOffset.UtcNow.Add(ChallengeLifetime));
        return code;
    }

    public bool TryCompleteRegistration(string email, string code, [NotNullWhen(true)] out PendingRegistration? registration)
    {
        registration = null;
        if (!_registrations.TryGetValue(email, out var pending) ||
            pending.ExpiresAt <= DateTimeOffset.UtcNow ||
            !HashesMatch(pending.CodeHash, Hash(code)))
        {
            return false;
        }

        if (!_registrations.TryRemove(email, out var completed) || completed is null)
        {
            return false;
        }

        registration = completed;
        return true;
    }

    public string? ResendRegistrationCode(string email)
    {
        if (!_registrations.TryGetValue(email, out var pending) || pending.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _registrations[email] = pending with { CodeHash = Hash(code), ExpiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime) };
        return code;
    }

    public string CreatePasswordReset(Guid userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        _passwordResets[Hash(token)] = new PasswordResetTicket(userId, DateTimeOffset.UtcNow.Add(ChallengeLifetime));
        return token;
    }

    public bool TryConsumePasswordReset(string token, out Guid userId)
    {
        userId = Guid.Empty;
        if (!_passwordResets.TryRemove(Hash(token), out var ticket) || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        userId = ticket.UserId;
        return true;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool HashesMatch(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expected),
            Convert.FromHexString(actual));

    public sealed record PendingRegistration(
        string Username,
        string Email,
        string CompanyName,
        string PasswordHash,
        string CodeHash,
        DateTimeOffset ExpiresAt);

    private sealed record PasswordResetTicket(Guid UserId, DateTimeOffset ExpiresAt);
}

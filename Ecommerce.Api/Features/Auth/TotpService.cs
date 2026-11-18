namespace Ecommerce.Api.Features.Auth;

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

public sealed class TotpService
{
    private const int Digits = 6;
    private const int StepSeconds = 30;
    private readonly IDataProtector _protector;

    public TotpService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Ecommerce.Api.MfaSecret.v1");
    }

    public string CreateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(20));

    public string Protect(string secret) => _protector.Protect(secret);

    public string Unprotect(string protectedSecret) => _protector.Unprotect(protectedSecret);

    public string CreateProvisioningUri(string email, string secret)
    {
        var label = Uri.EscapeDataString($"Ecommerce:{email}");
        var issuer = Uri.EscapeDataString("Ecommerce");
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&digits={Digits}&period={StepSeconds}";
    }

    public bool Verify(string secret, string? code, DateTimeOffset? now = null)
    {
        if (code is null || code.Length != Digits || code.Any(character => character is < '0' or > '9'))
        {
            return false;
        }

        var key = Base32Decode(secret);
        var currentStep = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / StepSeconds;
        var supplied = Encoding.ASCII.GetBytes(code);
        for (var adjustment = -1; adjustment <= 1; adjustment++)
        {
            var counter = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(counter, currentStep + adjustment);
            var digest = HMACSHA1.HashData(key, counter);
            var offset = digest[^1] & 0x0f;
            var binaryCode = ((digest[offset] & 0x7f) << 24)
                | ((digest[offset + 1] & 0xff) << 16)
                | ((digest[offset + 2] & 0xff) << 8)
                | (digest[offset + 3] & 0xff);
            var expected = Encoding.ASCII.GetBytes((binaryCode % 1_000_000).ToString("D6"));
            if (CryptographicOperations.FixedTimeEquals(supplied, expected))
            {
                return true;
            }
        }

        return false;
    }

    private static string Base32Encode(ReadOnlySpan<byte> data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output.Append(alphabet[(buffer << (5 - bits)) & 31]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>(value.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;
        foreach (var character in value.TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(character);
            if (index < 0)
            {
                throw new FormatException("The stored MFA secret is invalid.");
            }

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }

        return output.ToArray();
    }
}

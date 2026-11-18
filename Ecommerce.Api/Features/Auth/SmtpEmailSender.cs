namespace Ecommerce.Api.Features.Auth;

using System.Net;
using System.Net.Mail;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private string? Host => configuration["Email:Smtp:Host"];
    private string? From => configuration["Email:Smtp:From"];
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SMTP email delivery is not configured.");
        }

        var message = new MailMessage(From!, recipient, subject, body);
        using var client = new SmtpClient(Host!, int.TryParse(configuration["Email:Smtp:Port"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) || enableSsl,
            Credentials = string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"])
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(configuration["Email:Smtp:Username"], configuration["Email:Smtp:Password"])
        };
        using (message)
        {
            await client.SendMailAsync(message, cancellationToken);
        }
    }
}

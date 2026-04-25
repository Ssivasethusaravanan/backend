namespace identity_service.Services;

/// <summary>
/// Abstraction over any email delivery provider (SendGrid, Mailgun, SES, etc.).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}

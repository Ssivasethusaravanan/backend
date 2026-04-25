using SendGrid;
using SendGrid.Helpers.Mail;

namespace identity_service.Services;

/// <summary>
/// IEmailSender implementation backed by the SendGrid Web API.
/// Reads the API key from the Email:ApiKey config section or SENDGRID_API_KEY env var.
/// </summary>
public class SendGridEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                     ?? _configuration["Email:ApiKey"]
                     ?? throw new InvalidOperationException("SendGrid API key is not configured.");

        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@billflow.com";
        var fromName = _configuration["Email:FromName"] ?? "BillFlow";

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromAddress, fromName);
        var to = new EmailAddress(toEmail, toName);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

        var response = await client.SendEmailAsync(msg, ct);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError("SendGrid returned {StatusCode} for email to {Email}: {Body}",
                response.StatusCode, toEmail, body);
            throw new InvalidOperationException($"Email delivery failed with status {response.StatusCode}.");
        }

        _logger.LogInformation("Email '{Subject}' sent to {Email} via SendGrid.", subject, toEmail);
    }
}

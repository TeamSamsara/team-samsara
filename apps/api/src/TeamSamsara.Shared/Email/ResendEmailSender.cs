// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/ResendEmailSender.cs
// Version : 1.1.0
// Latest commit: feat/email-message-attachments
// Author : Gerrah
// Purpose : IEmailSender implementation backed by the Resend transactional email API.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TeamSamsara.Shared.Email;

public class ResendEmailSender : IEmailSender
{
    #region Fields

    private const string SendEmailPath = "/emails";

    // Most recipients Resend accepts on one email
    private const int MaxRecipients = 50;

    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    #endregion

    #region Constructors

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<EmailSettings> settings,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Sends one HTML email to a single recipient
    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            To = new[] { to },
            Subject = subject,
            HtmlBody = htmlBody
        };

        return SendEmailAsync(message, cancellationToken);
    }

    // Sends one HTML email to one or more recipients, with an optional reply address and attachments
    public async Task SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.To.Count is 0 or > MaxRecipients)
        {
            throw new ArgumentException(
                $"An email needs between 1 and {MaxRecipients} recipients.", nameof(message));
        }

        var request = new ResendEmailRequest
        {
            From = _settings.FromAddress,
            To = message.To.ToArray(),
            Subject = message.Subject,
            Html = message.HtmlBody,
            ReplyTo = message.ReplyTo,
            Attachments = message.Attachments.Count == 0
                ? null
                : message.Attachments.Select(ToResendAttachment).ToArray()
        };

        using var response = await _httpClient
            .PostAsJsonAsync(SendEmailPath, request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogError(
                "Resend email send failed. Status: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);

            throw new InvalidOperationException(
                $"Failed to send email via Resend (status {(int)response.StatusCode}).");
        }
    }

    #endregion

    #region Private Methods

    // Converts an attachment to Resend's shape, with the file content as base64
    private static ResendAttachment ToResendAttachment(EmailAttachment attachment)
    {
        return new ResendAttachment
        {
            FileName = attachment.FileName,
            Content = Convert.ToBase64String(attachment.Content)
        };
    }

    #endregion

    #region Nested Types

    private class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string[] To { get; set; } = Array.Empty<string>();

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("html")]
        public string Html { get; set; } = string.Empty;

        [JsonPropertyName("reply_to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReplyTo { get; set; }

        [JsonPropertyName("attachments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResendAttachment[]? Attachments { get; set; }
    }

    private class ResendAttachment
    {
        [JsonPropertyName("filename")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    #endregion
}

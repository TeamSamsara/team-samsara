// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/ResendEmailSender.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
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

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var request = new ResendEmailRequest
        {
            From = _settings.FromAddress,
            To = new[] { to },
            Subject = subject,
            Html = htmlBody
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
    }

    #endregion
}

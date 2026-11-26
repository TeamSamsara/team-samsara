// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Models/ContactSettings.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Binds the Contact module's configuration: recipients per category, attachment limits and the per-IP rate limit.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Contact.Models;

public class ContactSettings
{
    #region Fields

    public const string SectionName = "Contact";

    private const int BytesPerMebibyte = 1024 * 1024;
    private const char RecipientSeparator = ',';

    private const int DefaultMaxFiles = 5;
    private const int DefaultMaxFileBytes = 5 * BytesPerMebibyte;
    private const int DefaultMaxTotalBytes = 8 * BytesPerMebibyte;
    private const int DefaultRateLimitPermitLimit = 5;
    private const int DefaultRateLimitWindowSeconds = 600;

    private const int MaxFilesCeiling = 10;
    private const int MaxBytesCeiling = 10 * BytesPerMebibyte;
    private const int RateLimitPermitCeiling = 1000;
    private const int RateLimitWindowCeiling = 86400;

    #endregion

    #region Properties

    // Comma-separated email addresses that receive Sponsorship messages
    [Required]
    public string SponsorshipRecipients { get; set; } = string.Empty;

    // Comma-separated email addresses that receive General inquiries messages
    [Required]
    public string GeneralInquiriesRecipients { get; set; } = string.Empty;

    // Comma-separated email addresses that receive Media messages
    [Required]
    public string MediaRecipients { get; set; } = string.Empty;

    // Comma-separated email addresses that receive Customer service messages
    [Required]
    public string CustomerServiceRecipients { get; set; } = string.Empty;

    // Most files one submission may attach
    [Range(1, MaxFilesCeiling)]
    public int MaxFiles { get; set; } = DefaultMaxFiles;

    // Largest size of one attached file, in bytes
    [Range(1, MaxBytesCeiling)]
    public int MaxFileBytes { get; set; } = DefaultMaxFileBytes;

    // Largest combined size of all attached files, in bytes
    [Range(1, MaxBytesCeiling)]
    public int MaxTotalBytes { get; set; } = DefaultMaxTotalBytes;

    // Submissions one IP address may make per window
    [Range(1, RateLimitPermitCeiling)]
    public int RateLimitPermitLimit { get; set; } = DefaultRateLimitPermitLimit;

    // Length of the rate limit window, in seconds
    [Range(1, RateLimitWindowCeiling)]
    public int RateLimitWindowSeconds { get; set; } = DefaultRateLimitWindowSeconds;

    #endregion

    #region Public Methods

    // Lists the trimmed, non-empty addresses configured for the category
    public IReadOnlyList<string> GetRecipients(ContactCategory category)
    {
        var configured = category switch
        {
            ContactCategory.Sponsorship => SponsorshipRecipients,
            ContactCategory.GeneralInquiries => GeneralInquiriesRecipients,
            ContactCategory.Media => MediaRecipients,
            ContactCategory.CustomerService => CustomerServiceRecipients,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "No recipients for this category.")
        };

        return configured
            .Split(RecipientSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    #endregion
}

// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Handlers/ContactEmailBuilder.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Turns a validated contact submission into the staff notification email, with every visitor value HTML-encoded.

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Email;

namespace TeamSamsara.Modules.Contact.Handlers;

public class ContactEmailBuilder
{
    #region Fields

    private const string TokenPattern = @"\{\{(\w+)\}\}";

    private static readonly Regex _tokenRegex = new(TokenPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly ContactSettings _settings;
    private readonly IClock _clock;

    #endregion

    #region Constructors

    public ContactEmailBuilder(IOptions<ContactSettings> settings, IClock clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    #endregion

    #region Public Methods

    // Builds the email for a submission that already passed validation, addressed to its category's recipients
    public EmailMessage Build(ContactSubmission submission, ContactCategory category, IReadOnlyList<ContactFile> files)
    {
        var name = $"{submission.FirstName?.Trim()} {submission.LastName?.Trim()}";
        var email = submission.Email?.Trim() ?? string.Empty;
        var phone = submission.Phone?.Trim();
        var categoryLabel = ContactConstants.CategoryLabels[category];

        var template = ContactMessages.Body.Replace(
            "{{" + ContactMessages.PhoneRowKey + "}}",
            string.IsNullOrEmpty(phone) ? string.Empty : ContactMessages.PhoneRow,
            StringComparison.Ordinal
        );

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ContactMessages.CategoryKey] = WebUtility.HtmlEncode(categoryLabel),
            [ContactMessages.NameKey] = WebUtility.HtmlEncode(name),
            [ContactMessages.EmailKey] = WebUtility.HtmlEncode(email),
            [ContactMessages.PhoneKey] = WebUtility.HtmlEncode(phone ?? string.Empty),
            [ContactMessages.ReceivedKey] = _clock.UtcNow.UtcDateTime.ToString(ContactMessages.ReceivedFormat),
            [ContactMessages.MessageKey] = WebUtility.HtmlEncode(submission.Message?.Trim() ?? string.Empty)
        };

        return new EmailMessage
        {
            To = _settings.GetRecipients(category),
            Subject = string.Format(ContactMessages.SubjectFormat, categoryLabel, name),
            HtmlBody = Render(template, values),
            ReplyTo = email,
            Attachments = files.Select(file => new EmailAttachment(file.FileName, file.Content)).ToArray()
        };
    }

    #endregion

    #region Private Methods

    // Fills every token in one pass so a visitor value that looks like a token is never replaced again
    private static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        return _tokenRegex.Replace(
            template,
            match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value
        );
    }

    #endregion
}

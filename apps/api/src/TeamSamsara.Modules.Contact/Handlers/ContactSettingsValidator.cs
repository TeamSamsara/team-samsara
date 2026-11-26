// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Handlers/ContactSettingsValidator.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Fails startup when a category has no valid recipient address or when the attachment size limits contradict each other.

using System.Net.Mail;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Handlers;

public class ContactSettingsValidator : IValidateOptions<ContactSettings>
{
    #region Fields

    private const int MaxRecipientsPerCategory = 50;

    #endregion

    #region Public Methods

    // Checks every category's recipient list and the relation between the two size limits
    public ValidateOptionsResult Validate(string? name, ContactSettings options)
    {
        var failures = new List<string>();

        foreach (var category in Enum.GetValues<ContactCategory>())
        {
            ValidateRecipients(category, options.GetRecipients(category), failures);
        }

        if (options.MaxFileBytes > options.MaxTotalBytes)
        {
            failures.Add(
                $"{ContactSettings.SectionName}:{nameof(ContactSettings.MaxFileBytes)} must not exceed "
                    + $"{ContactSettings.SectionName}:{nameof(ContactSettings.MaxTotalBytes)}."
            );
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    #endregion

    #region Private Methods

    // Adds a failure when the category has no address, too many, or an address that cannot be parsed
    private static void ValidateRecipients(ContactCategory category, IReadOnlyList<string> recipients, List<string> failures)
    {
        if (recipients.Count == 0)
        {
            failures.Add($"{ContactSettings.SectionName}: {category} needs at least one recipient address.");
            return;
        }

        if (recipients.Count > MaxRecipientsPerCategory)
        {
            failures.Add(
                $"{ContactSettings.SectionName}: {category} has {recipients.Count} recipients, the maximum is {MaxRecipientsPerCategory}."
            );
        }

        foreach (var recipient in recipients)
        {
            if (!MailAddress.TryCreate(recipient, out _))
            {
                failures.Add($"{ContactSettings.SectionName}: {category} has an invalid recipient address '{recipient}'.");
            }
        }
    }

    #endregion
}

// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Handlers/ContactSubmissionValidator.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Checks the text fields of a contact submission and resolves its category.

using System.Net.Mail;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Handlers;

public class ContactSubmissionValidator
{
    #region Fields

    private const string AllowedPhoneSymbols = "+-(). ";

    #endregion

    #region Public Methods

    // Returns null when the submission is valid, otherwise the status describing the first problem found
    public ContactStatus? Validate(ContactSubmission submission, out ContactCategory category)
    {
        category = default;

        var firstName = submission.FirstName?.Trim();
        var lastName = submission.LastName?.Trim();
        var email = submission.Email?.Trim();
        var categoryValue = submission.Category?.Trim();
        var message = submission.Message?.Trim();

        if (
            string.IsNullOrEmpty(firstName)
            || string.IsNullOrEmpty(lastName)
            || string.IsNullOrEmpty(email)
            || string.IsNullOrEmpty(categoryValue)
            || string.IsNullOrEmpty(message)
        )
        {
            return ContactStatus.InvalidRequest;
        }

        if (!IsValidSingleLine(firstName, ContactConstants.MaxNameLength) || !IsValidSingleLine(lastName, ContactConstants.MaxNameLength))
        {
            return ContactStatus.InvalidField;
        }

        if (!IsValidEmail(email) || !IsValidPhone(submission.Phone?.Trim()))
        {
            return ContactStatus.InvalidField;
        }

        if (!IsValidMessage(message) || !ContactConstants.CategoryByValue.TryGetValue(categoryValue, out category))
        {
            return ContactStatus.InvalidField;
        }

        return null;
    }

    #endregion

    #region Private Methods

    // True when the value fits the limit and holds no control character
    private static bool IsValidSingleLine(string value, int maxLength)
    {
        return value.Length <= maxLength && !value.Any(char.IsControl);
    }

    // True when the value is one plain address with no display name, within the length limit
    private static bool IsValidEmail(string email)
    {
        return IsValidSingleLine(email, ContactConstants.MaxEmailLength)
            && MailAddress.TryCreate(email, out var address)
            && string.Equals(address.Address, email, StringComparison.Ordinal);
    }

    // True when the phone is absent or made only of digits and common phone symbols
    private static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return true;
        }

        return phone.Length <= ContactConstants.MaxPhoneLength
            && phone.All(character => char.IsAsciiDigit(character) || AllowedPhoneSymbols.Contains(character));
    }

    // True when the message fits the limit and holds no control character other than line breaks and tabs
    private static bool IsValidMessage(string message)
    {
        return message.Length <= ContactConstants.MaxMessageLength
            && message.All(character => !char.IsControl(character) || character is '\r' or '\n' or '\t');
    }

    #endregion
}

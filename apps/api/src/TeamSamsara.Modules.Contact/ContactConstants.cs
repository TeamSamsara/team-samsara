// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/ContactConstants.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : The Contact module's route, form field names, category values and field length limits.

using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact;

public static class ContactConstants
{
    #region Fields

    public const string Route = "/contact";
    public const string RateLimitPolicyName = "Contact";

    public const string FirstNameField = "firstName";
    public const string LastNameField = "lastName";
    public const string EmailField = "email";
    public const string PhoneField = "phone";
    public const string CategoryField = "category";
    public const string MessageField = "message";
    public const string FilesField = "files";

    // Hidden on the page; people leave it empty, form-filling bots do not
    public const string HoneypotField = "website";

    public const string SponsorshipValue = "sponsorship";
    public const string GeneralInquiriesValue = "general-inquiries";
    public const string MediaValue = "media";
    public const string CustomerServiceValue = "customer-service";

    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 254;
    public const int MaxPhoneLength = 30;
    public const int MaxMessageLength = 5000;
    public const int MaxFileNameLength = 100;

    #endregion

    #region Properties

    // The category each value the form may send stands for
    public static IReadOnlyDictionary<string, ContactCategory> CategoryByValue { get; } =
        new Dictionary<string, ContactCategory>(StringComparer.OrdinalIgnoreCase)
        {
            [SponsorshipValue] = ContactCategory.Sponsorship,
            [GeneralInquiriesValue] = ContactCategory.GeneralInquiries,
            [MediaValue] = ContactCategory.Media,
            [CustomerServiceValue] = ContactCategory.CustomerService
        };

    // How each category is named in the email the team receives
    public static IReadOnlyDictionary<ContactCategory, string> CategoryLabels { get; } =
        new Dictionary<ContactCategory, string>
        {
            [ContactCategory.Sponsorship] = "Sponsorship",
            [ContactCategory.GeneralInquiries] = "General inquiries",
            [ContactCategory.Media] = "Media",
            [ContactCategory.CustomerService] = "Customer service"
        };

    #endregion
}

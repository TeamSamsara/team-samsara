// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactSettingsValidatorTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the startup check accepts good settings and rejects missing or invalid recipients and contradictory size limits.

using Shouldly;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactSettingsValidatorTests
{
    #region Fields

    private const string Address = "staff@example.com";
    private const string SecondAddress = "media@example.com";
    private const string NotAnAddress = "not-an-address";
    private const int TooManyRecipients = 51;
    private const int SmallLimit = 1000;
    private const int LargeLimit = 2000;

    private readonly ContactSettingsValidator _validator = new();

    #endregion

    #region Public Methods

    [Fact]
    public void ValidSettings_Succeed()
    {
        var result = _validator.Validate(null, CreateSettings());

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void SeveralAddressesInOneCategory_Succeed()
    {
        var settings = CreateSettings();
        settings.MediaRecipients = $"{Address}, {SecondAddress}";

        _validator.Validate(null, settings).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ContactCategory.Sponsorship)]
    [InlineData(ContactCategory.GeneralInquiries)]
    [InlineData(ContactCategory.Media)]
    [InlineData(ContactCategory.CustomerService)]
    public void ACategoryWithoutRecipients_Fails(ContactCategory category)
    {
        var settings = CreateSettings();
        SetRecipients(settings, category, " , ");

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(category.ToString());
    }

    [Fact]
    public void AnInvalidAddress_Fails()
    {
        var settings = CreateSettings();
        settings.SponsorshipRecipients = $"{Address},{NotAnAddress}";

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(NotAnAddress);
    }

    [Fact]
    public void MoreRecipientsThanTheSenderAllows_Fails()
    {
        var settings = CreateSettings();
        settings.GeneralInquiriesRecipients = string.Join(",", Enumerable.Range(0, TooManyRecipients).Select(index => $"user{index}@example.com"));

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(ContactCategory.GeneralInquiries.ToString());
    }

    [Fact]
    public void AFileLimitAboveTheTotalLimit_Fails()
    {
        var settings = CreateSettings();
        settings.MaxFileBytes = LargeLimit;
        settings.MaxTotalBytes = SmallLimit;

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(ContactSettings.MaxFileBytes));
    }

    [Fact]
    public void SeveralProblems_AreAllReported()
    {
        var settings = CreateSettings();
        settings.SponsorshipRecipients = string.Empty;
        settings.MediaRecipients = NotAnAddress;

        var result = _validator.Validate(null, settings);

        result.Failures.ShouldNotBeNull().Count().ShouldBe(2);
    }

    #endregion

    #region Private Methods

    // Settings every test starts from, with one valid address per category
    private static ContactSettings CreateSettings()
    {
        return new ContactSettings
        {
            SponsorshipRecipients = Address,
            GeneralInquiriesRecipients = Address,
            MediaRecipients = Address,
            CustomerServiceRecipients = Address
        };
    }

    // Replaces the recipients of one category
    private static void SetRecipients(ContactSettings settings, ContactCategory category, string recipients)
    {
        switch (category)
        {
            case ContactCategory.Sponsorship:
                settings.SponsorshipRecipients = recipients;
                break;
            case ContactCategory.GeneralInquiries:
                settings.GeneralInquiriesRecipients = recipients;
                break;
            case ContactCategory.Media:
                settings.MediaRecipients = recipients;
                break;
            default:
                settings.CustomerServiceRecipients = recipients;
                break;
        }
    }

    #endregion
}

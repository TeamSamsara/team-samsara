// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactSubmissionValidatorTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the submission validator accepts good text fields, resolves the category, and rejects missing or malformed values.

using Shouldly;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactSubmissionValidatorTests
{
    #region Fields

    private const string ValidPhone = "+1 (514) 555-0123";
    private const string ControlCharacter = "\u0001";

    private readonly ContactSubmissionValidator _validator = new();

    #endregion

    #region Public Methods

    public static TheoryData<ContactSubmission> MissingFields()
    {
        var valid = CreateSubmission();

        return new TheoryData<ContactSubmission>
        {
            valid with { FirstName = null },
            valid with { FirstName = "   " },
            valid with { LastName = string.Empty },
            valid with { Email = null },
            valid with { Category = " " },
            valid with { Message = null },
            valid with { Message = "  \r\n " }
        };
    }

    public static TheoryData<ContactSubmission> MalformedFields()
    {
        var valid = CreateSubmission();

        return new TheoryData<ContactSubmission>
        {
            valid with { Category = "newsletter" },
            valid with { Email = "visitor" },
            valid with { Email = "Visitor <visitor@example.com>" },
            valid with { Email = "visitor@example.com\r\nBcc: other@example.com" },
            valid with { Email = new string('a', ContactConstants.MaxEmailLength) + "@example.com" },
            valid with { FirstName = $"Ada{ControlCharacter}" },
            valid with { LastName = "Line\nBreak" },
            valid with { FirstName = new string('a', ContactConstants.MaxNameLength + 1) },
            valid with { LastName = new string('a', ContactConstants.MaxNameLength + 1) },
            valid with { Phone = "call me" },
            valid with { Phone = new string('1', ContactConstants.MaxPhoneLength + 1) },
            valid with { Message = new string('a', ContactConstants.MaxMessageLength + 1) },
            valid with { Message = $"Hello{ControlCharacter}" }
        };
    }

    [Fact]
    public void AValidSubmission_PassesAndResolvesItsCategory()
    {
        var status = _validator.Validate(CreateSubmission(), out var category);

        status.ShouldBeNull();
        category.ShouldBe(ContactCategory.Media);
    }

    [Theory]
    [InlineData("sponsorship", ContactCategory.Sponsorship)]
    [InlineData("general-inquiries", ContactCategory.GeneralInquiries)]
    [InlineData("media", ContactCategory.Media)]
    [InlineData("customer-service", ContactCategory.CustomerService)]
    [InlineData("  Customer-Service ", ContactCategory.CustomerService)]
    public void EveryCategoryValue_ResolvesToItsCategory(string value, ContactCategory expected)
    {
        var status = _validator.Validate(CreateSubmission() with { Category = value }, out var category);

        status.ShouldBeNull();
        category.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(MissingFields))]
    public void AMissingRequiredField_IsAnInvalidRequest(ContactSubmission submission)
    {
        _validator.Validate(submission, out _).ShouldBe(ContactStatus.InvalidRequest);
    }

    [Theory]
    [MemberData(nameof(MalformedFields))]
    public void AMalformedField_IsAnInvalidField(ContactSubmission submission)
    {
        _validator.Validate(submission, out _).ShouldBe(ContactStatus.InvalidField);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(ValidPhone)]
    public void AnAbsentOrWellFormedPhone_Passes(string? phone)
    {
        _validator.Validate(CreateSubmission() with { Phone = phone }, out _).ShouldBeNull();
    }

    [Fact]
    public void ValuesAtTheirLengthLimits_Pass()
    {
        var submission = CreateSubmission() with
        {
            FirstName = new string('a', ContactConstants.MaxNameLength),
            LastName = new string('b', ContactConstants.MaxNameLength),
            Phone = new string('1', ContactConstants.MaxPhoneLength),
            Message = new string('c', ContactConstants.MaxMessageLength)
        };

        _validator.Validate(submission, out _).ShouldBeNull();
    }

    [Fact]
    public void AMessageWithLineBreaksAndTabs_Passes()
    {
        var submission = CreateSubmission() with { Message = "Line one\r\nLine two\tindented" };

        _validator.Validate(submission, out _).ShouldBeNull();
    }

    [Fact]
    public void SurroundingWhitespace_IsIgnored()
    {
        var submission = CreateSubmission() with { FirstName = "  Ada ", Email = " ada@example.com " };

        _validator.Validate(submission, out _).ShouldBeNull();
    }

    #endregion

    #region Private Methods

    // A submission that passes every check, which each test then spoils in one way
    private static ContactSubmission CreateSubmission()
    {
        return new ContactSubmission
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = ValidPhone,
            Category = ContactConstants.MediaValue,
            Message = "Hello team"
        };
    }

    #endregion
}

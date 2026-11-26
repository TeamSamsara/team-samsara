// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactEmailBuilderTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the email builder routes by category, sets subject and reply address, shows only provided fields, and never lets visitor text become markup or template tokens.

using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Modules.Contact.Tests.Fakes;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactEmailBuilderTests
{
    #region Fields

    private const string SponsorshipAddress = "sponsors@example.com";
    private const string GeneralAddress = "general@example.com";
    private const string MediaAddress = "media@example.com";
    private const string SecondMediaAddress = "press@example.com";
    private const string SupportAddress = "support@example.com";
    private const string VisitorEmail = "ada@example.com";
    private const string Phone = "+1 (514) 555-0123";
    private const string ReceivedText = "2026-01-15 22:45 UTC";
    private const string TokenOpening = "{{";

    private readonly FakeClock _clock = new();
    private readonly ContactEmailBuilder _builder;

    #endregion

    #region Constructors

    public ContactEmailBuilderTests()
    {
        var settings = new ContactSettings
        {
            SponsorshipRecipients = SponsorshipAddress,
            GeneralInquiriesRecipients = GeneralAddress,
            MediaRecipients = $"{MediaAddress}, {SecondMediaAddress}",
            CustomerServiceRecipients = SupportAddress
        };

        _builder = new ContactEmailBuilder(Options.Create(settings), _clock);
    }

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(ContactCategory.Sponsorship, SponsorshipAddress)]
    [InlineData(ContactCategory.GeneralInquiries, GeneralAddress)]
    [InlineData(ContactCategory.CustomerService, SupportAddress)]
    public void TheEmail_GoesToTheCategorysRecipients(ContactCategory category, string expected)
    {
        var message = _builder.Build(CreateSubmission(), category, []);

        message.To.ShouldBe([expected]);
    }

    [Fact]
    public void ACategoryWithSeveralRecipients_SendsOneEmailToAll()
    {
        var message = _builder.Build(CreateSubmission(), ContactCategory.Media, []);

        message.To.ShouldBe([MediaAddress, SecondMediaAddress]);
    }

    [Fact]
    public void TheSubject_NamesTheCategoryAndTheVisitor()
    {
        var message = _builder.Build(CreateSubmission(), ContactCategory.GeneralInquiries, []);

        message.Subject.ShouldBe("New contact form: General inquiries - Ada Lovelace");
    }

    [Fact]
    public void TheReplyAddress_IsTheVisitorsEmail()
    {
        var message = _builder.Build(CreateSubmission() with { Email = $"  {VisitorEmail} " }, ContactCategory.Media, []);

        message.ReplyTo.ShouldBe(VisitorEmail);
    }

    [Fact]
    public void TheBody_ShowsTheVisitorsDetailsAndTheReceivedTime()
    {
        var body = _builder.Build(CreateSubmission(), ContactCategory.Media, []).HtmlBody;

        body.ShouldContain("Ada Lovelace");
        body.ShouldContain(VisitorEmail);
        body.ShouldContain("Media");
        body.ShouldContain("Hello team");
        body.ShouldContain(ReceivedText);
        body.ShouldNotContain(TokenOpening);
    }

    [Fact]
    public void ThePhoneRow_AppearsWhenAPhoneWasGiven()
    {
        var body = _builder.Build(CreateSubmission(), ContactCategory.Media, []).HtmlBody;

        body.ShouldContain("Phone");
        body.ShouldContain(Phone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ThePhoneRow_IsLeftOutWhenNoPhoneWasGiven(string? phone)
    {
        var body = _builder.Build(CreateSubmission() with { Phone = phone }, ContactCategory.Media, []).HtmlBody;

        body.ShouldNotContain("Phone");
        body.ShouldNotContain(TokenOpening);
    }

    [Fact]
    public void MarkupInVisitorText_IsEncoded()
    {
        var submission = CreateSubmission() with
        {
            FirstName = "<script>alert(1)</script>",
            Message = "<img src=x onerror=alert(1)>"
        };

        var body = _builder.Build(submission, ContactCategory.Media, []).HtmlBody;

        body.ShouldNotContain("<script>");
        body.ShouldNotContain("<img");
        body.ShouldContain("&lt;script&gt;");
        body.ShouldContain("&lt;img");
    }

    [Fact]
    public void ATokenTypedByTheVisitor_IsShownAsTextNotReplaced()
    {
        var submission = CreateSubmission() with { Message = "my address is {{email}}" };

        var body = _builder.Build(submission, ContactCategory.Media, []).HtmlBody;

        body.ShouldContain("my address is {{email}}");
    }

    [Fact]
    public void LineBreaksInTheMessage_AreKept()
    {
        var submission = CreateSubmission() with { Message = "Line one\nLine two" };

        var body = _builder.Build(submission, ContactCategory.Media, []).HtmlBody;

        body.ShouldContain("Line one\nLine two");
    }

    [Fact]
    public void TheReceivedTime_FollowsTheClock()
    {
        _clock.UtcNow = new DateTimeOffset(2027, 3, 4, 5, 6, 7, TimeSpan.Zero);

        var body = _builder.Build(CreateSubmission(), ContactCategory.Media, []).HtmlBody;

        body.ShouldContain("2027-03-04 05:06 UTC");
    }

    [Fact]
    public void TheFiles_AreAttachedWithTheirNamesAndContent()
    {
        var content = new byte[] { 1, 2, 3 };

        var message = _builder.Build(CreateSubmission(), ContactCategory.Media, [new ContactFile("deck.pdf", content)]);

        message.Attachments.Count.ShouldBe(1);
        message.Attachments[0].FileName.ShouldBe("deck.pdf");
        message.Attachments[0].Content.ShouldBe(content);
    }

    [Fact]
    public void NoFiles_MeansNoAttachments()
    {
        _builder.Build(CreateSubmission(), ContactCategory.Media, []).Attachments.ShouldBeEmpty();
    }

    #endregion

    #region Private Methods

    // A validated submission the tests build the email from
    private static ContactSubmission CreateSubmission()
    {
        return new ContactSubmission
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = VisitorEmail,
            Phone = Phone,
            Category = ContactConstants.MediaValue,
            Message = "Hello team"
        };
    }

    #endregion
}

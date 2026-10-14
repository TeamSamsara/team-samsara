// File : /team-samsara/apps/api/src/TeamSamsara.Shared.Tests/ResendEmailSenderTests.cs
// Version : 1.0.0
// Latest commit: feat/email-message-attachments
// Author : Gerrah
// Purpose : Proves the Resend sender builds the right request for single and multi-recipient emails with reply address and attachments, and refuses bad recipient counts or a rejected send.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Shared.Email;
using TeamSamsara.Shared.Tests.Fakes;

namespace TeamSamsara.Shared.Tests;

public class ResendEmailSenderTests
{
    #region Fields

    private const string BaseAddress = "https://api.resend.com";
    private const string SendPath = "/emails";
    private const string FromAddress = "Team Samsara <alerts@example.com>";
    private const string Recipient = "staff@example.com";
    private const string SecondRecipient = "media@example.com";
    private const string Subject = "New contact form";
    private const string Html = "<p>Hello</p>";
    private const string ReplyToAddress = "visitor@example.com";
    private const string FileName = "deck.pdf";
    private const int MaxRecipients = 50;

    private static readonly byte[] _fileContent = { 1, 2, 3, 250 };

    private readonly StubHttpMessageHandler _handler = new();
    private readonly ResendEmailSender _sender;

    #endregion

    #region Constructors

    public ResendEmailSenderTests()
    {
        var client = new HttpClient(_handler) { BaseAddress = new Uri(BaseAddress) };
        var settings = Options.Create(new EmailSettings { ApiKey = "test-key", FromAddress = FromAddress });

        _sender = new ResendEmailSender(client, settings, NullLogger<ResendEmailSender>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task AMessage_IsPostedOnceToTheSendPath()
    {
        await _sender.SendEmailAsync(CreateMessage(Recipient));

        _handler.RequestCount.ShouldBe(1);
        _handler.Method.ShouldBe(HttpMethod.Post);
        _handler.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe(SendPath);
    }

    [Fact]
    public async Task AMessage_SendsTheFromAddressSubjectAndHtml()
    {
        await _sender.SendEmailAsync(CreateMessage(Recipient));

        var json = SentJson();
        json.GetProperty("from").GetString().ShouldBe(FromAddress);
        json.GetProperty("subject").GetString().ShouldBe(Subject);
        json.GetProperty("html").GetString().ShouldBe(Html);
    }

    [Fact]
    public async Task AMessage_WithSeveralRecipients_SendsThemAll()
    {
        await _sender.SendEmailAsync(CreateMessage(Recipient, SecondRecipient));

        var recipients = SentJson().GetProperty("to").EnumerateArray().Select(e => e.GetString());
        recipients.ShouldBe(new[] { Recipient, SecondRecipient });
    }

    [Fact]
    public async Task AMessage_WithAReplyAddress_SendsReplyTo()
    {
        var message = CreateMessage(Recipient) with { ReplyTo = ReplyToAddress };

        await _sender.SendEmailAsync(message);

        SentJson().GetProperty("reply_to").GetString().ShouldBe(ReplyToAddress);
    }

    [Fact]
    public async Task AMessage_WithAttachments_SendsEachFileNameAndBase64Content()
    {
        var message = CreateMessage(Recipient) with
        {
            Attachments = new[] { new EmailAttachment(FileName, _fileContent) }
        };

        await _sender.SendEmailAsync(message);

        var attachment = SentJson().GetProperty("attachments").EnumerateArray().ShouldHaveSingleItem();
        attachment.GetProperty("filename").GetString().ShouldBe(FileName);
        attachment.GetProperty("content").GetString().ShouldBe(Convert.ToBase64String(_fileContent));
    }

    [Fact]
    public async Task AMessage_WithoutAReplyAddressOrAttachments_OmitsBothFields()
    {
        await _sender.SendEmailAsync(CreateMessage(Recipient));

        var json = SentJson();
        json.TryGetProperty("reply_to", out _).ShouldBeFalse();
        json.TryGetProperty("attachments", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task AMessage_WithExactlyTheMostRecipients_IsSent()
    {
        await _sender.SendEmailAsync(CreateMessage(ManyRecipients(MaxRecipients)));

        _handler.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task AMessage_WithMoreThanTheMostRecipients_ThrowsAndSendsNothing()
    {
        var message = CreateMessage(ManyRecipients(MaxRecipients + 1));

        await Should.ThrowAsync<ArgumentException>(() => _sender.SendEmailAsync(message));

        _handler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task AMessage_WithNoRecipients_ThrowsAndSendsNothing()
    {
        var message = CreateMessage();

        await Should.ThrowAsync<ArgumentException>(() => _sender.SendEmailAsync(message));

        _handler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task ASingleAddressEmail_SendsOneRecipientWithoutExtras()
    {
        await _sender.SendEmailAsync(Recipient, Subject, Html);

        var json = SentJson();
        json.GetProperty("to").EnumerateArray().ShouldHaveSingleItem().GetString().ShouldBe(Recipient);
        json.GetProperty("subject").GetString().ShouldBe(Subject);
        json.TryGetProperty("reply_to", out _).ShouldBeFalse();
        json.TryGetProperty("attachments", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ARejectedSend_Throws()
    {
        _handler.StatusCode = HttpStatusCode.UnprocessableEntity;

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sender.SendEmailAsync(CreateMessage(Recipient)));
    }

    #endregion

    #region Private Methods

    // Builds a message to the given recipients with the standard subject and body
    private static EmailMessage CreateMessage(params string[] recipients)
    {
        return new EmailMessage
        {
            To = recipients,
            Subject = Subject,
            HtmlBody = Html
        };
    }

    // Lists the given number of distinct recipient addresses
    private static string[] ManyRecipients(int count)
    {
        return Enumerable.Range(1, count).Select(i => $"member{i}@example.com").ToArray();
    }

    // Parses the JSON body the sender posted
    private JsonElement SentJson()
    {
        var body = _handler.Body.ShouldNotBeNull();

        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    #endregion
}

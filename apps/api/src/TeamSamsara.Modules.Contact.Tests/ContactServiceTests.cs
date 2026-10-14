// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactServiceTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the contact service sends valid submissions, silently drops honeypot hits, rejects bad input before sending, and reports delivery failures.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Modules.Contact.Tests.Fakes;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactServiceTests
{
    #region Fields

    private const string MediaAddress = "media@example.com";
    private const string SupportAddress = "support@example.com";
    private const string OtherAddress = "other@example.com";
    private const int MaxFiles = 2;
    private const int MaxFileBytes = 100;
    private const int MaxTotalBytes = 150;
    private const int FileLength = 20;

    private static readonly byte[] _pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private readonly RecordingEmailSender _sender = new();
    private readonly ContactService _service;

    #endregion

    #region Constructors

    public ContactServiceTests()
    {
        var settings = Options.Create(new ContactSettings
        {
            SponsorshipRecipients = OtherAddress,
            GeneralInquiriesRecipients = OtherAddress,
            MediaRecipients = MediaAddress,
            CustomerServiceRecipients = SupportAddress,
            MaxFiles = MaxFiles,
            MaxFileBytes = MaxFileBytes,
            MaxTotalBytes = MaxTotalBytes
        });

        _service = new ContactService(
            new ContactSubmissionValidator(),
            new ContactAttachmentChecker(settings),
            new ContactEmailBuilder(settings, new FakeClock()),
            _sender,
            NullLogger<ContactService>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task AValidSubmission_IsSentOnce()
    {
        var status = await _service.SubmitAsync(CreateSubmission(), CancellationToken.None);

        status.ShouldBe(ContactStatus.Sent);
        _sender.Messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TheEmail_GoesToTheChosenCategorysRecipients()
    {
        var submission = CreateSubmission() with { Category = ContactConstants.CustomerServiceValue };

        await _service.SubmitAsync(submission, CancellationToken.None);

        _sender.Messages[0].To.ShouldBe([SupportAddress]);
    }

    [Fact]
    public async Task TheEmail_RepliesToTheVisitor()
    {
        await _service.SubmitAsync(CreateSubmission(), CancellationToken.None);

        _sender.Messages[0].ReplyTo.ShouldBe("ada@example.com");
    }

    [Fact]
    public async Task AFilledHoneypot_LooksLikeSuccessButSendsNothing()
    {
        var status = await _service.SubmitAsync(CreateSubmission() with { Honeypot = "https://spam.example" }, CancellationToken.None);

        status.ShouldBe(ContactStatus.Sent);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFilledHoneypot_IsDroppedEvenWhenTheRestIsInvalid()
    {
        var submission = new ContactSubmission { Honeypot = "bot" };

        var status = await _service.SubmitAsync(submission, CancellationToken.None);

        status.ShouldBe(ContactStatus.Sent);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ABlankHoneypot_IsTreatedAsEmpty()
    {
        var status = await _service.SubmitAsync(CreateSubmission() with { Honeypot = "  " }, CancellationToken.None);

        status.ShouldBe(ContactStatus.Sent);
        _sender.Messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AMissingField_IsAnInvalidRequestAndSendsNothing()
    {
        var status = await _service.SubmitAsync(CreateSubmission() with { Message = null }, CancellationToken.None);

        status.ShouldBe(ContactStatus.InvalidRequest);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task AMalformedField_IsAnInvalidFieldAndSendsNothing()
    {
        var status = await _service.SubmitAsync(CreateSubmission() with { Email = "visitor" }, CancellationToken.None);

        status.ShouldBe(ContactStatus.InvalidField);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task TooManyFiles_SendsNothing()
    {
        var files = Enumerable.Range(0, MaxFiles + 1).Select(index => CreatePng($"photo{index}.png")).ToArray();

        var status = await _service.SubmitAsync(CreateSubmission() with { Files = files }, CancellationToken.None);

        status.ShouldBe(ContactStatus.TooManyFiles);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnUnsupportedFile_SendsNothing()
    {
        var files = new[] { new ContactFile("program.exe", new byte[FileLength]) };

        var status = await _service.SubmitAsync(CreateSubmission() with { Files = files }, CancellationToken.None);

        status.ShouldBe(ContactStatus.UnsupportedFileType);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATooLargeFile_SendsNothing()
    {
        var files = new[] { new ContactFile("photo.png", new byte[MaxFileBytes + 1]) };

        var status = await _service.SubmitAsync(CreateSubmission() with { Files = files }, CancellationToken.None);

        status.ShouldBe(ContactStatus.FileTooLarge);
        _sender.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheFiles_AreAttachedWithCleanedNames()
    {
        var files = new[] { CreatePng("..\\folder\\photo.png") };

        await _service.SubmitAsync(CreateSubmission() with { Files = files }, CancellationToken.None);

        _sender.Messages[0].Attachments.Count.ShouldBe(1);
        _sender.Messages[0].Attachments[0].FileName.ShouldBe("photo.png");
    }

    [Theory]
    [MemberData(nameof(SenderFailures))]
    public async Task AFailedSend_IsADeliveryFailure(Exception failure)
    {
        _sender.ExceptionToThrow = failure;

        var status = await _service.SubmitAsync(CreateSubmission(), CancellationToken.None);

        status.ShouldBe(ContactStatus.DeliveryFailed);
    }

    [Fact]
    public async Task ACancelledRequest_IsNotReportedAsADeliveryFailure()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        _sender.ExceptionToThrow = new OperationCanceledException(source.Token);

        await Should.ThrowAsync<OperationCanceledException>(() => _service.SubmitAsync(CreateSubmission(), source.Token));
    }

    public static TheoryData<Exception> SenderFailures()
    {
        return new TheoryData<Exception>
        {
            new InvalidOperationException("Resend rejected the email."),
            new HttpRequestException("Network down."),
            new TaskCanceledException("Timed out.")
        };
    }

    #endregion

    #region Private Methods

    // A submission that passes every check
    private static ContactSubmission CreateSubmission()
    {
        return new ContactSubmission
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Category = ContactConstants.MediaValue,
            Message = "Hello team"
        };
    }

    // A small file that starts like a real PNG
    private static ContactFile CreatePng(string fileName)
    {
        var content = new byte[FileLength];
        _pngSignature.CopyTo(content, 0);

        return new ContactFile(fileName, content);
    }

    #endregion
}

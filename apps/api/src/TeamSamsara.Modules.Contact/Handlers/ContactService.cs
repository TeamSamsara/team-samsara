// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Handlers/ContactService.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Handles one contact submission: drops honeypot hits, validates text and files, then emails the team.

using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Contact.Models;
using TeamSamsara.Shared.Email;

namespace TeamSamsara.Modules.Contact.Handlers;

public class ContactService : IContactService
{
    #region Fields

    private readonly ContactSubmissionValidator _submissionValidator;
    private readonly ContactAttachmentChecker _attachmentChecker;
    private readonly ContactEmailBuilder _emailBuilder;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ContactService> _logger;

    #endregion

    #region Constructors

    public ContactService(
        ContactSubmissionValidator submissionValidator,
        ContactAttachmentChecker attachmentChecker,
        ContactEmailBuilder emailBuilder,
        IEmailSender emailSender,
        ILogger<ContactService> logger
    )
    {
        _submissionValidator = submissionValidator;
        _attachmentChecker = attachmentChecker;
        _emailBuilder = emailBuilder;
        _emailSender = emailSender;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Validates the submission and emails it to the team, returning the outcome
    public async Task<ContactStatus> SubmitAsync(ContactSubmission submission, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(submission.Honeypot))
        {
            _logger.LogInformation("Contact submission dropped: honeypot field was filled.");
            return ContactStatus.Sent;
        }

        var textStatus = _submissionValidator.Validate(submission, out var category);
        if (textStatus is not null)
        {
            return textStatus.Value;
        }

        var fileStatus = _attachmentChecker.Check(submission.Files, out var files);
        if (fileStatus is not null)
        {
            return fileStatus.Value;
        }

        var message = _emailBuilder.Build(submission, category, files);

        try
        {
            await _emailSender.SendEmailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Contact email for category {Category} could not be delivered.", category);
            return ContactStatus.DeliveryFailed;
        }

        _logger.LogInformation("Contact email for category {Category} sent with {FileCount} attachment(s).", category, files.Count);
        return ContactStatus.Sent;
    }

    #endregion
}

// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/EmailChangeDelivery.cs
// Version : 1.2.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Issues the two email change codes in the background. The address was free when the request
// was made, so a decoy is only a backstop for one taken since. Every step is logged, masked.

using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Identity.Handlers;

public class EmailChangeDelivery : IEmailChangeDelivery
{
    #region Fields

    private readonly IAccountGateway _accounts;
    private readonly IVerificationCodeService _verification;
    private readonly ILogger<EmailChangeDelivery> _logger;

    #endregion

    #region Constructors

    // Initializes the delivery with the account gateway and code service.
    public EmailChangeDelivery(
        IAccountGateway accounts,
        IVerificationCodeService verification,
        ILogger<EmailChangeDelivery> logger)
    {
        _accounts = accounts;
        _verification = verification;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Emails a code to the current address, and one to the new address unless it is unavailable.
    public async Task DeliverAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var currentEmail = await _accounts.GetEmailAsync(userId);

        if (currentEmail is null)
        {
            _logger.LogWarning("Email change codes for user {UserId} not issued: the account has no email.", userId);

            return;
        }

        var available = await IsAvailableAsync(newEmail);

        await RunLoggedAsync(
            () => IssueAsync(
                userId,
                VerificationPurpose.EmailChangeOld,
                currentEmail,
                new Dictionary<string, string> { [AlertTemplateKeys.NewEmail] = EmailMasking.Mask(newEmail) },
                cancellationToken),
            userId,
            "the current-address code");

        await RunLoggedAsync(
            () => available
                ? IssueAsync(userId, VerificationPurpose.EmailChangeNew, newEmail, null, cancellationToken)
                : IssueDecoyAsync(userId, newEmail, cancellationToken),
            userId,
            "the new-address code");
    }

    #endregion

    #region Private Methods

    // An address is unavailable if any account holds it, including the member's own.
    private async Task<bool> IsAvailableAsync(string email)
    {
        return await _accounts.GetUserIdByEmailAsync(email) is null;
    }

    // Generates a code and emails it, with any extra values the email shows.
    private async Task IssueAsync(
        string userId,
        VerificationPurpose purpose,
        string email,
        IReadOnlyDictionary<string, string>? extraData,
        CancellationToken cancellationToken)
    {
        var result = await _verification.IssueAsync(userId, purpose, email, cancellationToken, extraData: extraData);

        _logger.LogInformation(
            "The {Purpose} code for user {UserId} to {Email}: {Status}.",
            purpose, userId, EmailMasking.Mask(email), result.Status);
    }

    // Stores a new-address code that is never sent, because another account took the address
    // after the request was accepted.
    private async Task IssueDecoyAsync(string userId, string newEmail, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Email change for user {UserId}: {NewEmail} was taken after the request; a decoy code is stored and nothing is sent.",
            userId, EmailMasking.Mask(newEmail));

        await _verification.IssueDecoyAsync(userId, VerificationPurpose.EmailChangeNew, cancellationToken);
    }

    // A failed send must not stop the other code; the member can request again.
    private async Task RunLoggedAsync(Func<Task> work, string userId, string description)
    {
        try
        {
            await work();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Could not issue {Description} for an email change by user {UserId}: {Reason}.",
                description,
                userId,
                exception.GetType().Name);
        }
    }

    #endregion
}

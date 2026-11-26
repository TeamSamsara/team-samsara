// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/EmailChangeService.cs
// Version : 1.3.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Email change for signed-in members. A new address that is taken, or is already theirs,
// is refused straight away; every step is logged with the address masked.

using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.BackgroundTasks;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class EmailChangeService : IEmailChangeService
{
    #region Fields

    private const int MaxEmailLength = 254;

    private readonly IBackgroundTaskQueue _queue;
    private readonly IVerificationCodeService _verification;
    private readonly IEmailChangeRequestStore _requests;
    private readonly IUserStore _users;
    private readonly IAccountGateway _accounts;
    private readonly ISignInTracker _signIns;
    private readonly IAlertSender _alerts;
    private readonly IClock _clock;
    private readonly VerificationSettings _settings;
    private readonly ILogger<EmailChangeService> _logger;

    #endregion

    #region Constructors

    // Initializes the service with its stores, queue, alert sender and settings.
    public EmailChangeService(
        IBackgroundTaskQueue queue,
        IVerificationCodeService verification,
        IEmailChangeRequestStore requests,
        IUserStore users,
        IAccountGateway accounts,
        ISignInTracker signIns,
        IAlertSender alerts,
        IClock clock,
        IOptions<VerificationSettings> settings,
        ILogger<EmailChangeService> logger)
    {
        _queue = queue;
        _verification = verification;
        _requests = requests;
        _users = users;
        _accounts = accounts;
        _signIns = signIns;
        _alerts = alerts;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Refuses an unusable new address straight away; otherwise saves the pending change and
    // queues the codes. A refusal saves nothing, sends nothing and starts no cooldown.
    public async Task<EmailChangeCodeResult> RequestChangeAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var currentEmail = await FindMemberEmailAsync(userId);

        if (currentEmail is null)
        {
            return Rejected(EmailChangeStatus.AccountNotFound);
        }

        var address = (newEmail ?? string.Empty).Trim();
        var refusal = await CheckAddressAsync(currentEmail, address);

        if (refusal != EmailChangeStatus.Success)
        {
            LogRequestRefused(userId, address, refusal);

            return Rejected(refusal);
        }

        var wait = await RemainingCooldownSecondsAsync(userId);

        if (wait > 0)
        {
            _logger.LogInformation(
                "Email change for user {UserId} to {NewEmail} held back by the cooldown for {WaitSeconds}s.",
                userId, EmailMasking.Mask(address), wait);

            return new EmailChangeCodeResult(
                EmailChangeStatus.CooldownActive, _settings.EmailChangeCodeLength, wait);
        }

        await SaveRequestAsync(userId, address);
        QueueDelivery(userId, address);

        _logger.LogInformation(
            "Email change for user {UserId} to {NewEmail} accepted; the codes are queued.",
            userId, EmailMasking.Mask(address));

        return new EmailChangeCodeResult(
            EmailChangeStatus.Success, _settings.EmailChangeCodeLength, _settings.ResendCooldownSeconds);
    }

    // Checks the codes sent; once both are accepted, switches the address, ends every session and alerts the old address.
    public async Task<EmailChangeStatus> ConfirmAsync(
        string userId,
        string? oldCode,
        string? newCode,
        CancellationToken cancellationToken = default)
    {
        var currentEmail = await FindMemberEmailAsync(userId);

        if (currentEmail is null)
        {
            return EmailChangeStatus.AccountNotFound;
        }

        var pending = await GetActiveRequestAsync(userId);

        if (pending is null)
        {
            return EmailChangeStatus.NoPendingRequest;
        }

        var codes = await CheckCodesAsync(pending, oldCode, newCode, cancellationToken);

        if (codes != EmailChangeStatus.Success)
        {
            _logger.LogInformation("Email change codes for user {UserId} checked: {Status}.", userId, codes);

            return codes;
        }

        return await CompleteAsync(userId, currentEmail, cancellationToken);
    }

    #endregion

    #region Private Methods

    // The email of a completed member account, or null when there is none to act on.
    private async Task<string?> FindMemberEmailAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user is not { AccessLevel: AccessLevel.Member })
        {
            return null;
        }

        return await _accounts.GetEmailAsync(userId);
    }

    // Says whether the new address can be used: a plain address, not the member's own, and not
    // held by another account. Success means it can.
    private async Task<EmailChangeStatus> CheckAddressAsync(string currentEmail, string address)
    {
        if (!IsValidEmail(address))
        {
            return EmailChangeStatus.InvalidEmail;
        }

        if (string.Equals(address, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            return EmailChangeStatus.SameAsCurrentEmail;
        }

        return await _accounts.GetUserIdByEmailAsync(address) is null
            ? EmailChangeStatus.Success
            : EmailChangeStatus.EmailTaken;
    }

    // Logs why a request was refused, with the address masked; nothing the member typed is logged in full.
    private void LogRequestRefused(string userId, string address, EmailChangeStatus refusal)
    {
        _logger.LogInformation(
            "Email change for user {UserId} to {NewEmail} refused: {Status}.",
            userId, EmailMasking.Mask(address), refusal);
    }

    // Accepts a plain address of reasonable length, without display names or comments.
    private static bool IsValidEmail(string address)
    {
        return address.Length is > 0 and <= MaxEmailLength
            && MailAddress.TryCreate(address, out var parsed)
            && parsed.Address == address;
    }

    // Seconds until another request is allowed; depends only on the member's own last request.
    private async Task<int> RemainingCooldownSecondsAsync(string userId)
    {
        var pending = await _requests.GetAsync(userId);

        if (pending is null)
        {
            return 0;
        }

        var remaining = TimeSpan.FromSeconds(_settings.ResendCooldownSeconds) - (_clock.UtcNow - pending.CreatedAt);

        return remaining > TimeSpan.Zero ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
    }

    // Stores a fresh request, replacing any earlier one and clearing its verified flags.
    private async Task SaveRequestAsync(string userId, string newEmail)
    {
        var now = _clock.UtcNow;

        await _requests.SaveAsync(new EmailChangeRequest
        {
            Id = userId,
            NewEmail = newEmail,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_settings.ExpiryMinutes)
        });
    }

    // Hands the availability check and the emails to the background queue.
    private void QueueDelivery(string userId, string newEmail)
    {
        var queued = _queue.TryEnqueue((services, cancellationToken) =>
            services.GetRequiredService<IEmailChangeDelivery>()
                .DeliverAsync(userId, newEmail, cancellationToken));

        if (!queued)
        {
            _logger.LogWarning("The background queue is full; an email change request was dropped.");
        }
    }

    // The member's pending request, or null if there is none; an expired one is removed.
    private async Task<EmailChangeRequest?> GetActiveRequestAsync(string userId)
    {
        var pending = await _requests.GetAsync(userId);

        if (pending is null)
        {
            return null;
        }

        if (_clock.UtcNow < pending.ExpiresAt)
        {
            return pending;
        }

        await _requests.DeleteAsync(userId);

        return null;
    }

    // Checks every code sent in this call. Success means both are now accepted; CodeAccepted means one is
    // accepted and the other has not been sent yet.
    private async Task<EmailChangeStatus> CheckCodesAsync(
        EmailChangeRequest pending,
        string? oldCode,
        string? newCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oldCode) && string.IsNullOrWhiteSpace(newCode))
        {
            return EmailChangeStatus.InvalidCode;
        }

        var old = await CheckCodeAsync(
            pending.Id, VerificationPurpose.EmailChangeOld, pending.OldCodeVerified, oldCode, cancellationToken);

        var added = await CheckCodeAsync(
            pending.Id, VerificationPurpose.EmailChangeNew, pending.NewCodeVerified, newCode, cancellationToken);

        if (old == VerificationResult.TooManyAttempts || added == VerificationResult.TooManyAttempts)
        {
            await _requests.DeleteAsync(pending.Id);

            return EmailChangeStatus.TooManyAttempts;
        }

        if (old is not (null or VerificationResult.Valid) || added is not (null or VerificationResult.Valid))
        {
            return EmailChangeStatus.InvalidCode;
        }

        return old == VerificationResult.Valid && added == VerificationResult.Valid
            ? EmailChangeStatus.Success
            : EmailChangeStatus.CodeAccepted;
    }

    // Verifies one code unless it was already accepted, and records the acceptance.
    // Null means no code was sent for it and none was accepted before.
    private async Task<VerificationResult?> CheckCodeAsync(
        string userId,
        VerificationPurpose purpose,
        bool alreadyVerified,
        string? code,
        CancellationToken cancellationToken)
    {
        if (alreadyVerified)
        {
            return VerificationResult.Valid;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var result = await _verification.VerifyAsync(userId, purpose, code.Trim(), cancellationToken);

        if (result == VerificationResult.Valid)
        {
            await _requests.MarkVerifiedAsync(userId, purpose);
        }

        return result;
    }

    // Consumes the request, switches the address and ends every session.
    private async Task<EmailChangeStatus> CompleteAsync(
        string userId,
        string currentEmail,
        CancellationToken cancellationToken)
    {
        var request = await _requests.TakeAsync(userId);

        if (request is null)
        {
            return EmailChangeStatus.NoPendingRequest;
        }

        // The address was free when the request was made but is not any more, so another account
        // took it in between. It is answered like a wrong code, which is what the member sees.
        if (await _accounts.GetUserIdByEmailAsync(request.NewEmail) is not null)
        {
            _logger.LogWarning(
                "Email change for user {UserId} to {NewEmail} could not complete: the address was taken meanwhile.",
                userId, EmailMasking.Mask(request.NewEmail));

            return EmailChangeStatus.InvalidCode;
        }

        await _accounts.SetEmailAsync(userId, request.NewEmail);
        await _accounts.RevokeSessionsAsync(userId);
        await _signIns.ClearVerifiedAsync(userId);
        await AlertEmailChangedAsync(currentEmail, request.NewEmail, cancellationToken);

        _logger.LogInformation(
            "Email change for user {UserId} completed: {OldEmail} to {NewEmail}; every session was ended.",
            userId, EmailMasking.Mask(currentEmail), EmailMasking.Mask(request.NewEmail));

        return EmailChangeStatus.Success;
    }

    // Best effort: a failed alert must not fail a completed change.
    private async Task AlertEmailChangedAsync(
        string oldEmail,
        string newEmail,
        CancellationToken cancellationToken)
    {
        var templateData = new Dictionary<string, string>
        {
            [AlertTemplateKeys.NewEmail] = EmailMasking.Mask(newEmail)
        };

        try
        {
            await _alerts.SendAlertAsync(oldEmail, AlertType.EmailChanged, templateData, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the email-changed alert.");
        }
    }

    // A request answer that carries no code details.
    private static EmailChangeCodeResult Rejected(EmailChangeStatus status)
    {
        return new EmailChangeCodeResult(status, 0, 0);
    }

    #endregion
}

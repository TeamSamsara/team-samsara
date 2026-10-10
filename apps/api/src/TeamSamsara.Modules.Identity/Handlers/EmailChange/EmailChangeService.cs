// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/EmailChangeService.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Email change for signed-in members, answering the same way whether the new address is free or taken.

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

    // Initializes the service with its stores, queue, notification channel and settings.
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

    // Saves the pending change and queues the codes; availability is only checked in the background.
    public async Task<EmailChangeCodeResult> RequestChangeAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        if (await FindMemberEmailAsync(userId) is null)
        {
            return Rejected(EmailChangeStatus.AccountNotFound);
        }

        var address = (newEmail ?? string.Empty).Trim();

        if (!IsValidEmail(address))
        {
            return Rejected(EmailChangeStatus.InvalidEmail);
        }

        var wait = await RemainingCooldownSecondsAsync(userId);

        if (wait > 0)
        {
            return new EmailChangeCodeResult(
                EmailChangeStatus.CooldownActive, _settings.EmailChangeCodeLength, wait);
        }

        await SaveRequestAsync(userId, address);
        QueueDelivery(userId, address);

        return new EmailChangeCodeResult(
            EmailChangeStatus.Success, _settings.EmailChangeCodeLength, _settings.ResendCooldownSeconds);
    }

    // Checks both codes, then switches the address, ends every session and notifies the old address.
    public async Task<EmailChangeStatus> ConfirmAsync(
        string userId,
        string oldCode,
        string newCode,
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

    // Checks both codes every time, so the answer and timing do not show which one failed.
    private async Task<EmailChangeStatus> CheckCodesAsync(
        EmailChangeRequest pending,
        string oldCode,
        string newCode,
        CancellationToken cancellationToken)
    {
        var old = await CheckCodeAsync(
            pending.Id, VerificationPurpose.EmailChangeOld, pending.OldCodeVerified, oldCode, cancellationToken);

        var added = await CheckCodeAsync(
            pending.Id, VerificationPurpose.EmailChangeNew, pending.NewCodeVerified, newCode, cancellationToken);

        if (old == VerificationResult.TooManyAttempts || added == VerificationResult.TooManyAttempts)
        {
            await _requests.DeleteAsync(pending.Id);

            return EmailChangeStatus.TooManyAttempts;
        }

        return old == VerificationResult.Valid && added == VerificationResult.Valid
            ? EmailChangeStatus.Success
            : EmailChangeStatus.InvalidCode;
    }

    // Verifies one code unless it was already accepted, and records the acceptance.
    private async Task<VerificationResult> CheckCodeAsync(
        string userId,
        VerificationPurpose purpose,
        bool alreadyVerified,
        string code,
        CancellationToken cancellationToken)
    {
        if (alreadyVerified)
        {
            return VerificationResult.Valid;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return VerificationResult.Invalid;
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

        // Answered like a wrong code, so a taken address is not revealed.
        if (await _accounts.GetUserIdByEmailAsync(request.NewEmail) is not null)
        {
            return EmailChangeStatus.InvalidCode;
        }

        await _accounts.SetEmailAsync(userId, request.NewEmail);
        await _accounts.RevokeSessionsAsync(userId);
        await _signIns.ClearVerifiedAsync(userId);
        await NotifyEmailChangedAsync(currentEmail, request.NewEmail, cancellationToken);

        return EmailChangeStatus.Success;
    }

    // Best effort: a failed notice must not fail a completed change.
    private async Task NotifyEmailChangedAsync(
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
            _logger.LogWarning(exception, "Could not send the email-changed notice.");
        }
    }

    // A request answer that carries no code details.
    private static EmailChangeCodeResult Rejected(EmailChangeStatus status)
    {
        return new EmailChangeCodeResult(status, 0, 0);
    }

    #endregion
}

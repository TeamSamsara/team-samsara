// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/VerificationCodeService.cs
// Version : 1.3.0
// Latest commit: feat/decoy-verification-code
// Author : Gerrah
// Purpose : Issues and checks one-time verification codes. Codes are delivered through
// IAlertSender, throttled on resend, and burned after too many wrong guesses. A decoy code
// is stored the same way but never delivered. Cryptography lives in VerificationCodeCrypto;
// this class is the workflow only.

using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class VerificationCodeService : IVerificationCodeService
{
    #region Fields

    private readonly IVerificationCodeStore _store;
    private readonly IAlertSender _alertSender;
    private readonly IClock _clock;
    private readonly VerificationSettings _settings;

    #endregion

    #region Constructors

    public VerificationCodeService(
        IVerificationCodeStore store,
        IAlertSender alertSender,
        IClock clock,
        IOptions<VerificationSettings> settings)
    {
        _store = store;
        _alertSender = alertSender;
        _clock = clock;
        _settings = settings.Value;
    }

    #endregion

    #region Public Methods

    // Generates, stores (hashed) and emails a code, unless one was sent too recently
    public Task<VerificationIssueResult> IssueAsync(
        string userId,
        VerificationPurpose purpose,
        string email,
        CancellationToken cancellationToken = default)
    {
        return IssueCoreAsync(
            userId,
            purpose,
            (profile, code) => SendCodeOrDiscardAsync(
                userId, purpose, email, profile.AlertType, code, cancellationToken));
    }

    // Does everything IssueAsync does except email the code. The stored code can never be
    // read, so it behaves like a real one that was lost: same cooldown, expiry and attempt limit.
    public Task<VerificationIssueResult> IssueDecoyAsync(
        string userId,
        VerificationPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return IssueCoreAsync(userId, purpose, (_, _) => Task.CompletedTask);
    }

    // Checks a submitted code and consumes it if valid
    public async Task<VerificationResult> VerifyAsync(
        string userId,
        VerificationPurpose purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        var stored = await _store.GetAsync(userId, purpose);

        if (stored is null)
        {
            return VerificationResult.NotFound;
        }

        if (_clock.UtcNow >= stored.ExpiresAt)
        {
            return await ConsumeAsync(userId, purpose, VerificationResult.Expired);
        }

        // Count the attempt BEFORE comparing, so parallel guesses each get a distinct count.
        var attempts = await _store.IncrementFailedAttemptsAsync(userId, purpose);

        if (attempts is null)
        {
            return VerificationResult.NotFound;
        }

        if (attempts > _settings.MaxFailedAttempts)
        {
            return await ConsumeAsync(userId, purpose, VerificationResult.TooManyAttempts);
        }

        if (VerificationCodeCrypto.Matches(code.Trim(), stored.Salt, stored.CodeHash))
        {
            return await ConsumeAsync(userId, purpose, VerificationResult.Valid);
        }

        return attempts >= _settings.MaxFailedAttempts
            ? await ConsumeAsync(userId, purpose, VerificationResult.TooManyAttempts)
            : VerificationResult.Invalid;
    }

    #endregion

    #region Private Methods

    // Applies the cooldown, stores a fresh code, then hands it to the delivery step
    private async Task<VerificationIssueResult> IssueCoreAsync(
        string userId,
        VerificationPurpose purpose,
        Func<PurposeProfile, string, Task> deliverAsync)
    {
        var profile = GetPurposeProfile(purpose);
        var retryAfterSeconds = await GetRemainingCooldownSecondsAsync(userId, purpose);

        if (retryAfterSeconds > 0)
        {
            return new VerificationIssueResult(
                VerificationIssueStatus.CooldownActive, profile.CodeLength, retryAfterSeconds);
        }

        var code = VerificationCodeCrypto.GenerateCode(profile.CodeLength);

        await SaveCodeAsync(userId, purpose, code);
        await deliverAsync(profile, code);

        return new VerificationIssueResult(
            VerificationIssueStatus.Sent, profile.CodeLength, _settings.ResendCooldownSeconds);
    }

    // The settings that vary by purpose, resolved in one place
    private PurposeProfile GetPurposeProfile(VerificationPurpose purpose)
    {
        return purpose switch
        {
            VerificationPurpose.Registration =>
                new PurposeProfile(_settings.RegistrationCodeLength, AlertType.RegistrationCode),

            VerificationPurpose.StepUp =>
                new PurposeProfile(_settings.StepUpCodeLength, AlertType.StepUpCode),

            // The login challenge reuses the step-up wording; the separate "new login attempt"
            // notice is sent by the login flow.
            VerificationPurpose.LoginChallenge =>
                new PurposeProfile(_settings.LoginChallengeCodeLength, AlertType.StepUpCode),

            // The reset code reuses the step-up wording; the separate "password reset requested"
            // notice is sent by the reset flow when the request comes from an unknown client.
            VerificationPurpose.PasswordReset =>
                new PurposeProfile(_settings.PasswordResetCodeLength, AlertType.StepUpCode),

            _ => throw new ArgumentOutOfRangeException(
                nameof(purpose), purpose, "Unknown verification purpose.")
        };
    }

    // Seconds until another code may be sent for this member and purpose, or 0 if one may be
    private async Task<int> GetRemainingCooldownSecondsAsync(string userId, VerificationPurpose purpose)
    {
        var existing = await _store.GetAsync(userId, purpose);

        if (existing is null)
        {
            return 0;
        }

        var canResendAt = existing.CreatedAt.AddSeconds(_settings.ResendCooldownSeconds);
        var remaining = canResendAt - _clock.UtcNow;

        return remaining > TimeSpan.Zero ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
    }

    // Stores a new code for the member, keeping only its salted hash
    private async Task SaveCodeAsync(string userId, VerificationPurpose purpose, string code)
    {
        var now = _clock.UtcNow;
        var salt = VerificationCodeCrypto.GenerateSalt();

        await _store.SaveAsync(new VerificationCode
        {
            Id = VerificationCode.BuildId(userId, purpose),
            UserId = userId,
            Purpose = purpose,
            CodeHash = VerificationCodeCrypto.Hash(code, salt),
            Salt = salt,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_settings.ExpiryMinutes)
        });
    }

    // Emails the code. If the email fails, the stored code is dropped - otherwise the
    // cooldown would make the member wait to retry for a message they never received.
    private async Task SendCodeOrDiscardAsync(
        string userId,
        VerificationPurpose purpose,
        string email,
        AlertType alertType,
        string code,
        CancellationToken cancellationToken)
    {
        var templateData = new Dictionary<string, string>
        {
            [AlertTemplateKeys.Code] = code
        };

        try
        {
            await _alertSender.SendAlertAsync(email, alertType, templateData, cancellationToken);
        }
        catch
        {
            await _store.DeleteAsync(userId, purpose);
            throw;
        }
    }

    // Deletes the pending code and returns the given result, so a code can never be reused
    private async Task<VerificationResult> ConsumeAsync(
        string userId,
        VerificationPurpose purpose,
        VerificationResult result)
    {
        await _store.DeleteAsync(userId, purpose);

        return result;
    }

    #endregion

    #region Nested Types

    private readonly record struct PurposeProfile(int CodeLength, AlertType AlertType);

    #endregion
}

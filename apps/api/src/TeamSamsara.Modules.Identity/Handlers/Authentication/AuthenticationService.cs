// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/AuthenticationService.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Default IAuthenticationService. A sign-in only becomes verified (and so honored as
// Member by the authentication handler) once the client is recognized or the emailed
// challenge code has been confirmed.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class AuthenticationService : IAuthenticationService
{
    #region Fields

    private readonly IUserStore _users;
    private readonly IAccountGateway _accounts;
    private readonly IVerificationCodeService _verification;
    private readonly IGuardDog _guardDog;
    private readonly ISignInTracker _signIns;
    private readonly IAlertSender _alerts;
    private readonly IClock _clock;
    private readonly AuthenticationSettings _settings;
    private readonly ILogger<AuthenticationService> _logger;

    #endregion

    #region Constructors

    public AuthenticationService(
        IUserStore users,
        IAccountGateway accounts,
        IVerificationCodeService verification,
        IGuardDog guardDog,
        ISignInTracker signIns,
        IAlertSender alerts,
        IClock clock,
        IOptions<AuthenticationSettings> settings,
        ILogger<AuthenticationService> logger)
    {
        _users = users;
        _accounts = accounts;
        _verification = verification;
        _guardDog = guardDog;
        _signIns = signIns;
        _alerts = alerts;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Verifies the sign-in if the client is recognized, otherwise sends the challenge code
    public async Task<LoginResult> CheckSignInAsync(
        string userId,
        long authTime,
        CancellationToken cancellationToken = default)
    {
        var account = await LookUpAsync(userId);

        if (account.IsRejected)
        {
            return Rejected(account.Rejection);
        }

        var client = _guardDog.Inspect();

        if (IsRecognized(account.User, client))
        {
            await VerifySignInAsync(userId, authTime, client);

            return new LoginResult(AuthenticationStatus.Authenticated, 0, 0);
        }

        return await StartChallengeAsync(account.User, account.Email, client, cancellationToken);
    }

    // Checks the challenge code and, if valid, verifies the sign-in (restoring the account if
    // it had been deleted)
    public async Task<AuthenticationStatus> ConfirmChallengeAsync(
        string userId,
        long authTime,
        string code,
        CancellationToken cancellationToken = default)
    {
        var account = await LookUpAsync(userId);

        if (account.IsRejected)
        {
            return account.Rejection;
        }

        var verification = await _verification.VerifyAsync(
            userId, VerificationPurpose.LoginChallenge, code, cancellationToken);

        if (verification != VerificationResult.Valid)
        {
            return ToFailureStatus(verification);
        }

        await VerifySignInAsync(userId, authTime, _guardDog.Inspect());

        return AuthenticationStatus.Authenticated;
    }

    // Sends a fresh challenge code (no new-login alert: the member was already told once)
    public async Task<LoginResult> ResendChallengeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var account = await LookUpAsync(userId);

        if (account.IsRejected)
        {
            return Rejected(account.Rejection);
        }

        var issue = await _verification.IssueAsync(
            userId, VerificationPurpose.LoginChallenge, account.Email, cancellationToken);

        var status = issue.Status == VerificationIssueStatus.Sent
            ? AuthenticationStatus.ChallengeRequired
            : AuthenticationStatus.CooldownActive;

        return new LoginResult(status, issue.CodeLength, issue.RetryAfterSeconds);
    }

    #endregion

    #region Private Methods

    // Loads the member and decides whether they can sign in at all
    private async Task<AccountLookup> LookUpAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);
        var email = await _accounts.GetEmailAsync(userId);

        if (user is null || email is null)
        {
            return AccountLookup.Reject(AuthenticationStatus.AccountNotFound);
        }

        if (user.AccessLevel == AccessLevel.Guest)
        {
            return AccountLookup.Reject(AuthenticationStatus.RegistrationIncomplete);
        }

        if (IsPastRecoveryWindow(user))
        {
            return AccountLookup.Reject(AuthenticationStatus.AccountNotFound);
        }

        return AccountLookup.Accept(user, email);
    }

    // A deleted account can only be restored for a limited time; after that it awaits purging
    private bool IsPastRecoveryWindow(User user)
    {
        if (user.DeletedAt is not { } deletedAt)
        {
            return false;
        }

        return deletedAt.AddDays(_settings.RecoveryWindowDays) < _clock.UtcNow;
    }

    // Deleted accounts are never recognized: they must confirm a code to be restored
    private bool IsRecognized(User user, ClientInfo client)
    {
        return user.DeletedAt is null && _guardDog.IsKnown(user, client);
    }

    // Sends the challenge code, and tells the member about the attempt when a code went out
    private async Task<LoginResult> StartChallengeAsync(
        User user,
        string email,
        ClientInfo client,
        CancellationToken cancellationToken)
    {
        var issue = await _verification.IssueAsync(
            user.Id, VerificationPurpose.LoginChallenge, email, cancellationToken);

        if (issue.Status == VerificationIssueStatus.Sent)
        {
            await NotifyNewLoginAsync(email, client, cancellationToken);
        }

        return new LoginResult(
            AuthenticationStatus.ChallengeRequired, issue.CodeLength, issue.RetryAfterSeconds);
    }

    // Courtesy email about the attempt. The code email already went out, so a failure here is
    // logged and must not break the login.
    private async Task NotifyNewLoginAsync(
        string email,
        ClientInfo client,
        CancellationToken cancellationToken)
    {
        var templateData = new Dictionary<string, string>
        {
            [AlertTemplateKeys.IpAddress] = client.IpAddress,
            [AlertTemplateKeys.UserAgent] = client.UserAgent
        };

        try
        {
            await _alerts.SendAlertAsync(
                email, AlertType.NewLoginAttempt, templateData, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the new-login alert.");
        }
    }

    // Remembers the client, clears any deletion marker, then marks the sign-in as verified
    private async Task VerifySignInAsync(string userId, long authTime, ClientInfo client)
    {
        await _users.ModifyAsync(userId, user =>
        {
            user.DeletedAt = null;
            _guardDog.Remember(user, client);
        });

        await _signIns.RecordVerifiedAsync(userId, authTime);
    }

    // Translates a failed code check into an authentication outcome
    private static AuthenticationStatus ToFailureStatus(VerificationResult verification)
    {
        return verification switch
        {
            VerificationResult.Invalid => AuthenticationStatus.InvalidCode,
            VerificationResult.Expired => AuthenticationStatus.CodeExpired,
            VerificationResult.TooManyAttempts => AuthenticationStatus.TooManyAttempts,
            VerificationResult.NotFound => AuthenticationStatus.NoPendingCode,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verification), verification, "Not a failed verification result.")
        };
    }

    // An answer that carries no challenge details
    private static LoginResult Rejected(AuthenticationStatus status)
    {
        return new LoginResult(status, 0, 0);
    }

    #endregion

    #region Nested Types

    // Either an account allowed to sign in (User and Email set) or the reason it is not
    private sealed class AccountLookup
    {
        public User? User { get; private init; }
        public string? Email { get; private init; }
        public AuthenticationStatus Rejection { get; private init; }

        [MemberNotNullWhen(false, nameof(User), nameof(Email))]
        public bool IsRejected { get; private init; }

        public static AccountLookup Accept(User user, string email)
        {
            return new AccountLookup { User = user, Email = email };
        }

        public static AccountLookup Reject(AuthenticationStatus rejection)
        {
            return new AccountLookup { Rejection = rejection, IsRejected = true };
        }
    }

    #endregion
}

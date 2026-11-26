// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/AuthenticationService.cs
// Version : 1.4.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Default IAuthenticationService. A sign-in only becomes verified (and so honored as
// Member by the authentication handler) once the client is recognized or the emailed
// challenge code has been confirmed. An unrecognized client also leaves the member a notice (INewLoginNotifier).

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
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
    private readonly INewLoginNotifier _newLogins;
    private readonly AccountRecovery _recovery;

    #endregion

    #region Constructors

    public AuthenticationService(
        IUserStore users,
        IAccountGateway accounts,
        IVerificationCodeService verification,
        IGuardDog guardDog,
        ISignInTracker signIns,
        INewLoginNotifier newLogins,
        AccountRecovery recovery)
    {
        _users = users;
        _accounts = accounts;
        _verification = verification;
        _guardDog = guardDog;
        _signIns = signIns;
        _newLogins = newLogins;
        _recovery = recovery;
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

        if (!IsRecognized(account.User, client))
        {
            return await StartChallengeAsync(account.User, account.Email, client, cancellationToken);
        }

        return await VerifyRecognizedAsync(userId, authTime, client);
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

        if (!await VerifySignInAsync(userId, authTime, _guardDog.Inspect()))
        {
            return AuthenticationStatus.AccountNotFound;
        }

        return AuthenticationStatus.Authenticated;
    }

    // Sends a fresh challenge code (no new-login notice: the member was already told once)
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

        if (_recovery.IsPastWindow(user))
        {
            return AccountLookup.Reject(AuthenticationStatus.AccountNotFound);
        }

        return AccountLookup.Accept(user, email);
    }

    // Deleted accounts are never recognized: they must confirm a code to be restored
    private bool IsRecognized(User user, ClientInfo client)
    {
        return user.DeletedAt is null && _guardDog.IsKnown(user, client);
    }

    private async Task<LoginResult> VerifyRecognizedAsync(string userId, long authTime, ClientInfo client)
    {
        if (!await VerifySignInAsync(userId, authTime, client))
        {
            return Rejected(AuthenticationStatus.AccountNotFound);
        }

        return new LoginResult(AuthenticationStatus.Authenticated, 0, 0);
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
            await _newLogins.NotifyAsync(user.Id, email, client, cancellationToken);
        }

        return new LoginResult(
            AuthenticationStatus.ChallengeRequired, issue.CodeLength, issue.RetryAfterSeconds);
    }

    // Restores the account and records the sign-in in one write; false if the account can no longer be restored
    private async Task<bool> VerifySignInAsync(string userId, long authTime, ClientInfo client)
    {
        return await _signIns.RecordVerifiedAsync(userId, authTime, user => TryRestore(user, client));
    }

    // Brings the account back if allowed, and remembers the client
    private bool TryRestore(User user, ClientInfo client)
    {
        if (!_recovery.TryRestore(user))
        {
            return false;
        }

        _guardDog.Remember(user, client);

        return true;
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
}

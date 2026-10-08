// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/RegistrationService.cs
// Version : 1.0.1
// Latest commit: fix/neutral-default-display-name
// Author : Gerrah
// Purpose : Default IRegistrationService. Registration is complete only when the User record
// is flipped to Member, which is deliberately the last step so a partial failure leaves the
// account as a Guest who can simply confirm again.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class RegistrationService : IRegistrationService
{
    #region Fields

    private const string NeutralDisplayName = "Member";

    private readonly IUserStore _users;
    private readonly IProfileStore _profiles;
    private readonly IAccountGateway _accounts;
    private readonly IVerificationCodeService _verification;
    private readonly IGuardDog _guardDog;
    private readonly IClock _clock;

    #endregion

    #region Constructors

    public RegistrationService(
        IUserStore users,
        IProfileStore profiles,
        IAccountGateway accounts,
        IVerificationCodeService verification,
        IGuardDog guardDog,
        IClock clock)
    {
        _users = users;
        _profiles = profiles;
        _accounts = accounts;
        _verification = verification;
        _guardDog = guardDog;
        _clock = clock;
    }

    #endregion

    #region Public Methods

    // Records the account as a Guest (if new) and sends the first code
    public async Task<RegistrationStartResult> StartAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var email = await _accounts.GetEmailAsync(userId);

        if (email is null)
        {
            return Rejected(RegistrationStatus.AccountNotFound);
        }

        var user = await _users.GetByIdAsync(userId);

        if (user is not null && user.AccessLevel != AccessLevel.Guest)
        {
            return Rejected(RegistrationStatus.AlreadyRegistered);
        }

        user ??= await CreateGuestAsync(userId);

        return await IssueCodeAsync(userId, email, cancellationToken);
    }

    // Sends a fresh code to an account whose registration has begun but is not complete
    public async Task<RegistrationStartResult> ResendAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var email = await _accounts.GetEmailAsync(userId);
        var user = await _users.GetByIdAsync(userId);

        if (email is null || user is null)
        {
            return Rejected(RegistrationStatus.AccountNotFound);
        }

        if (user.AccessLevel != AccessLevel.Guest)
        {
            return Rejected(RegistrationStatus.AlreadyRegistered);
        }

        return await IssueCodeAsync(userId, email, cancellationToken);
    }

    // Checks the code and, if valid, completes registration
    public async Task<RegistrationStatus> ConfirmAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var email = await _accounts.GetEmailAsync(userId);
        var user = await _users.GetByIdAsync(userId);

        if (email is null || user is null)
        {
            return RegistrationStatus.AccountNotFound;
        }

        if (user.AccessLevel != AccessLevel.Guest)
        {
            return RegistrationStatus.AlreadyRegistered;
        }

        var verification = await _verification.VerifyAsync(
            userId, VerificationPurpose.Registration, code, cancellationToken);

        if (verification != VerificationResult.Valid)
        {
            return ToFailureStatus(verification);
        }

        await PromoteToMemberAsync(user);

        return RegistrationStatus.Success;
    }

    #endregion

    #region Private Methods

    // Records a brand-new account as a Guest awaiting confirmation
    private async Task<User> CreateGuestAsync(string userId)
    {
        var user = new User
        {
            Id = userId,
            AccessLevel = AccessLevel.Guest,
            CreatedAt = _clock.UtcNow
        };

        await _users.CreateAsync(user);

        return user;
    }

    // Requests a registration code and shapes the answer for the caller
    private async Task<RegistrationStartResult> IssueCodeAsync(
        string userId,
        string email,
        CancellationToken cancellationToken)
    {
        var issue = await _verification.IssueAsync(
            userId, VerificationPurpose.Registration, email, cancellationToken);

        var status = issue.Status == VerificationIssueStatus.Sent
            ? RegistrationStatus.Success
            : RegistrationStatus.CooldownActive;

        return new RegistrationStartResult(status, issue.CodeLength, issue.RetryAfterSeconds);
    }

    // Completes registration. The User record flips to Member LAST: everything before it can
    // be safely repeated, so a failure partway leaves a Guest who can confirm again.
    private async Task PromoteToMemberAsync(User user)
    {
        await EnsureProfileAsync(user.Id);

        await _accounts.SetAccessLevelAsync(user.Id, AccessLevel.Member);

        user.AccessLevel = AccessLevel.Member;
        _guardDog.Remember(user, _guardDog.Inspect());

        await _users.UpdateAsync(user);
    }

    // Creates the member's profile unless an earlier, partly failed attempt already did. It starts
    // with a neutral name, never one taken from the email, since profiles are visible to others;
    // the member can change it during onboarding.
    private async Task EnsureProfileAsync(string userId)
    {
        if (await _profiles.GetByIdAsync(userId) is not null)
        {
            return;
        }

        await _profiles.CreateAsync(new Profile
        {
            Id = userId,
            DisplayName = NeutralDisplayName
        });
    }

    // Translates a failed code check into a registration outcome
    private static RegistrationStatus ToFailureStatus(VerificationResult verification)
    {
        return verification switch
        {
            VerificationResult.Invalid => RegistrationStatus.InvalidCode,
            VerificationResult.Expired => RegistrationStatus.CodeExpired,
            VerificationResult.TooManyAttempts => RegistrationStatus.TooManyAttempts,
            VerificationResult.NotFound => RegistrationStatus.NoPendingCode,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verification), verification, "Not a failed verification result.")
        };
    }

    // A start/resend answer that carries no code details
    private static RegistrationStartResult Rejected(RegistrationStatus status)
    {
        return new RegistrationStartResult(status, 0, 0);
    }

    #endregion
}

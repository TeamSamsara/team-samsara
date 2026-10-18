// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IVerificationCodeService.cs
// Version : 1.2.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : Issues and checks the one-time verification codes shared by registration, step-up
// confirmation and the new-device login challenge.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IVerificationCodeService
{
    #region Public Methods

    // Generates a code for the member and purpose, stores only its hash, and emails it to
    // `email`. Returns CooldownActive without sending if a code was sent too recently.
    // `wording` picks the email when the purpose alone does not; `extraData` adds template values.
    public Task<VerificationIssueResult> IssueAsync(
        string userId,
        VerificationPurpose purpose,
        string email,
        CancellationToken cancellationToken = default,
        AlertType? wording = null,
        IReadOnlyDictionary<string, string>? extraData = null);

    // Stores a code exactly as IssueAsync does but never sends it, and returns the same answer.
    // Used where the caller must not reveal whether a member exists.
    public Task<VerificationIssueResult> IssueDecoyAsync(
        string userId,
        VerificationPurpose purpose,
        CancellationToken cancellationToken = default);

    // Checks a submitted code. A valid code is consumed and cannot be used again.
    public Task<VerificationResult> VerifyAsync(
        string userId,
        VerificationPurpose purpose,
        string code,
        CancellationToken cancellationToken = default);

    #endregion
}

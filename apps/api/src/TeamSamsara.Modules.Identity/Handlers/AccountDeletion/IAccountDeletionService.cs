// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/AccountDeletion/IAccountDeletionService.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : Deletes a signed-in member's account after they confirm a code sent to their email;
// the account stays restorable by signing in until the recovery window ends.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IAccountDeletionService
{
    #region Public Methods

    // Sends the confirmation code to the member's email
    public Task<AccountDeletionCodeResult> RequestDeletionCodeAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Checks the code and, if valid, marks the account deleted and ends every session. The code
    // works once, and only one request can win the deletion.
    public Task<AccountDeletionStatus> DeleteAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default);

    #endregion
}

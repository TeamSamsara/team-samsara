// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/AccountDeletion/IAccountPurgeService.cs
// Version : 1.0.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Permanently removes the accounts whose recovery window has ended, safely retryable and safe to run on several instances at once.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IAccountPurgeService
{
    #region Public Methods

    // Purges every expired deleted account it can claim; a failure on one account is counted and never stops the rest
    public Task<AccountPurgeResult> PurgeExpiredAsync(CancellationToken cancellationToken = default);

    #endregion
}

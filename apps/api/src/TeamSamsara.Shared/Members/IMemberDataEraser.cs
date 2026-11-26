// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Members/IMemberDataEraser.cs
// Version : 1.0.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : Contract a module implements so the account purge can remove everything that module holds about a member.

namespace TeamSamsara.Shared.Members;

public interface IMemberDataEraser
{
    #region Public Methods

    // Deletes everything the module holds for the member; safe to repeat, nothing left counts as success, and a failure must throw so the purge keeps the account and retries
    public Task EraseAsync(string userId, CancellationToken cancellationToken = default);

    #endregion
}

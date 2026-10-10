// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IEmailChangeRequestStore.cs
// Version : 1.0.0
// Latest commit: feat/email-change-request-store
// Author : Gerrah
// Purpose : Saves and reads each member's pending email change, independent of where it is stored.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IEmailChangeRequestStore
{
    #region Public Methods

    // Returns the member's pending change, or null if there is none
    public Task<EmailChangeRequest?> GetAsync(string userId);

    // Saves a request, replacing any pending one for the same member
    public Task SaveAsync(EmailChangeRequest request);

    // Atomically marks one code as confirmed (EmailChangeOld or EmailChangeNew); does nothing if
    // there is no pending change. Atomic so parallel confirmations cannot overwrite each other.
    public Task MarkVerifiedAsync(string userId, VerificationPurpose purpose);

    // Reads and deletes the request in one atomic step, so only one caller can complete the
    // change. Returns null if there is none.
    public Task<EmailChangeRequest?> TakeAsync(string userId);

    // Deletes the member's pending change
    public Task DeleteAsync(string userId);

    #endregion
}

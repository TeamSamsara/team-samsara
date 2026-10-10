// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IPasswordResetTokenStore.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-token-store
// Author : Gerrah
// Purpose : Saves password reset tokens and hands each one out exactly once, independent of
// where they are stored. Keyed by the hash of the token.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IPasswordResetTokenStore
{
    #region Public Methods

    // Saves a token record
    public Task SaveAsync(PasswordResetToken token);

    // Reads and deletes the record in one atomic step, so two requests presenting the same token
    // cannot both succeed. Returns null if no such record exists (unknown or already used).
    public Task<PasswordResetToken?> TakeAsync(string id);

    #endregion
}
